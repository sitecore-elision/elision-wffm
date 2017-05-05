using System;
using System.Collections.Generic;
using System.Linq;
using Elision.Feature.Library.Wffm.ActionFilters;
using Elision.Feature.Library.Wffm.Models;
using Sitecore.Data;
using Sitecore.Exceptions;
using Sitecore.Form.Core.Data;
using Sitecore.Forms.Core.Data;
using Sitecore.Form.Core.ContentEditor.Data;
using Sitecore.Forms.Core.Handlers;
using Sitecore.Links;
using Sitecore.WFFM.Abstractions.Actions;

namespace Elision.Feature.Library.Wffm
{    
    public interface IWfmFormSubmitter
    {
        WfmFormSubmitResults SubmitWfmForm<T>(T model, ID formId = null);
    }

    public class WfmFormSubmitter : IWfmFormSubmitter
    {
        private readonly Database _db;
        private readonly FormDataHandler _formDataHandler;

        public WfmFormSubmitter()
        {
            _formDataHandler = Sitecore.Configuration.Factory.CreateObject("wffm/formDataHandler", true) as FormDataHandler;
            _db = Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database;
        }

        public WfmFormSubmitResults SubmitWfmForm<T>(T model, ID formId = null)
        {
            FormItem formItem = null;

            if (ID.IsNullOrEmpty(formId))
            {
                var formAttribute = model.GetType().GetCustomAttributes(typeof(WfmFormAttribute), true).FirstOrDefault() as WfmFormAttribute;
                if (formAttribute != null)
                    formItem = _db.SelectSingleItem("//*[@@TemplateName='Form' and @@Name='" + formAttribute.FormName + "']");
            }
            else
            {
                formItem = _db.GetItem(formId);
            }

            if (formItem == null)
                throw new ItemNotFoundException("Form not found.");

            var controlResults = GetWfmValues(model, formItem).ToArray();
            var actions = GetActions(formItem);

            try
            {
                _formDataHandler.ProcessForm(formItem.ID, controlResults, actions);
                return new WfmFormSubmitResults
                {
                    Status = WfmFormSubmitStatus.Success,
                    SuccessAction = formItem.SuccessRedirect ? WfmSuccessAction.Redirect : WfmSuccessAction.Message,
                    SuccessMessage = formItem.SuccessMessage,
                    SuccessRedirect = LinkManager.GetItemUrl(_db.GetItem(formItem.SuccessPageID))
                };
            }
            catch (FormSubmitException fex)
            {
                return new WfmFormSubmitResults
                {
                    Status = WfmFormSubmitStatus.Failure,
                    Messages = fex.Failures.Select(x => x.ErrorMessage)
                };
            }
        }

        private IEnumerable<ControlResult> GetWfmValues<T>(T model, FormItem formItem)
        {
            var modelProperties = typeof(T).GetProperties();

            foreach (var field in formItem.Fields)
            {
                var property =
                    modelProperties.FirstOrDefault(
                        x => x.Name.Equals(field.Name.Replace(" ", ""), StringComparison.InvariantCultureIgnoreCase));

                var value = property == null ? string.Empty : property.GetValue(model);

                if (string.IsNullOrWhiteSpace((string)value) && field.IsRequired)
                    Sitecore.Diagnostics.Log.Error($"Required field '{field.ID}' does not contain a value", this);

                yield return new ControlResult(field.ID.ToString(), field.Name, value ?? string.Empty, string.Empty);
            }
        }

        private static IActionDefinition[] GetActions(FormItem form)
        {
            var list = new List<IActionDefinition>();
            var actionDefinitions = new[]
            {
                ListDefinition.Parse(form.SaveActions),
                ListDefinition.Parse(form.CheckActions)
            };

            foreach (var actionDefinition in actionDefinitions)
            {
                if (actionDefinition.Groups.Any() && actionDefinition.Groups.First().ListItems.Any())
                {
                    foreach (var groupDefinition in actionDefinition.Groups)
                        list.AddRange(
                            groupDefinition.ListItems.Select(li => new ActionDefinition(li.ItemID, li.Parameters)
                            {
                                UniqueKey = li.Unicid
                            }));
                }
            }            

            return list.ToArray();
        }
    }
}