namespace FluentValidation.Internal {
	using System;
	using Resources;
	using Validators;

	public class MessageBuilderContext<T,TProperty> : ICommonContext {
		private PropertyValidatorContext<T,TProperty> _innerContext;

		public MessageBuilderContext(PropertyValidatorContext<T,TProperty> innerContext, IPropertyValidator<T,TProperty> propertyValidator) {
			_innerContext = innerContext;
			PropertyValidator = propertyValidator;
			// TODO: For backwards compatibility (remove in FV10).
#pragma warning disable 618
			ErrorSource = PropertyValidator.Options.ErrorMessageSource;
#pragma warning restore 618
		}


		public IPropertyValidator<T,TProperty> PropertyValidator { get; }

		[Obsolete("This property is deprecated and will be removed in FluentValidation 10.")]
		public IStringSource ErrorSource { get; }

		public IValidationContext ParentContext => _innerContext.ParentContext;

		public PropertyRule<T,TProperty> Rule => _innerContext.Rule;

		public string PropertyName => _innerContext.PropertyName;

		public string DisplayName => _innerContext.DisplayName;

		public MessageFormatter MessageFormatter => _innerContext.MessageFormatter;

		public T InstanceToValidate => _innerContext.InstanceToValidate;
		public TProperty PropertyValue => _innerContext.PropertyValue;

		object ICommonContext.PropertyValue => PropertyValue;
		object ICommonContext.InstanceToValidate => InstanceToValidate;

		ICommonContext ICommonContext.ParentContext => ParentContext;

		public string GetDefaultMessage() {
			return PropertyValidator.Options.GetErrorMessageTemplate(_innerContext);
		}

		public static implicit operator PropertyValidatorContext<T,TProperty>(MessageBuilderContext<T,TProperty> ctx) {
			return ctx._innerContext;
		}

	}
}
