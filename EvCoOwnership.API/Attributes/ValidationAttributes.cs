using Microsoft.AspNetCore.Mvc;

namespace EvCoOwnership.API.Attributes
{
    /// <summary>
    /// Attribute to enable automatic FluentValidation for an action or controller
    /// This is applied globally by default, but can be used for explicit control
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ValidateModelAttribute : Attribute
    {
        /// <summary>
        /// Whether to validate the model for this action/controller
        /// </summary>
        public bool Enabled { get; set; } = true;

        public ValidateModelAttribute(bool enabled = true)
        {
            Enabled = enabled;
        }
    }

    /// <summary>
    /// Attribute to disable automatic FluentValidation for an action or controller
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SkipValidationAttribute : Attribute
    {
    }
}