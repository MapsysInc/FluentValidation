#region License
// Copyright (c) .NET Foundation and contributors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// The latest version of this file can be found at https://github.com/FluentValidation/FluentValidation
#endregion

namespace FluentValidation.Validators {
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using Internal;
	using Resources;
	using Results;

	public abstract class PropertyValidator<T,TProperty> : PropertyValidatorOptions<T,TProperty>, IPropertyValidator<T,TProperty> {

		/// <inheritdoc />
		//TODO: For FV 10 make this an explicit implementation.
		public PropertyValidatorOptions<T,TProperty> Options => this;

		protected PropertyValidator(string errorMessage) {
			SetErrorMessage(errorMessage);
		}

		protected PropertyValidator() {
		}

		/// <summary>
		/// Retrieves a localized string from the LanguageManager.
		/// If an ErrorCode is defined for this validator, the error code is used as the key.
		/// If no ErrorCode is defined (or the language manager doesn't have a translation for the error code)
		/// then the fallback key is used instead.
		/// </summary>
		/// <param name="fallbackKey">The fallback key to use for translation, if no ErrorCode is available.</param>
		/// <returns>The translated error message template.</returns>
		protected string Localized(string fallbackKey) {
			var errorCode = ErrorCode;

			if (errorCode != null) {
				string result = ValidatorOptions.Global.LanguageManager.GetString(errorCode);

				if (!string.IsNullOrEmpty(result)) {
					return result;
				}
			}

			return ValidatorOptions.Global.LanguageManager.GetString(fallbackKey);
		}


		/// <inheritdoc />
		public virtual IEnumerable<ValidationFailure> Validate(PropertyValidatorContext<T,TProperty> context) {
			if (IsValid(context)) return Enumerable.Empty<ValidationFailure>();

			PrepareMessageFormatterForValidationError(context);
			return new[] { CreateValidationError(context) };

		}

		/// <inheritdoc />
		public virtual async Task<IEnumerable<ValidationFailure>> ValidateAsync(PropertyValidatorContext<T,TProperty> context, CancellationToken cancellation) {
			if (await IsValidAsync(context, cancellation)) return Enumerable.Empty<ValidationFailure>();

			PrepareMessageFormatterForValidationError(context);
			return new[] {CreateValidationError(context)};
		}

		/// <inheritdoc />
		public virtual bool ShouldValidateAsynchronously(ValidationContext<T> context) {
			// If the user has applied an async condition, then always go through the async path
			// even if validator is being run synchronously.
			if (HasAsyncCondition) return true;
			return false;
		}

		protected abstract bool IsValid(PropertyValidatorContext<T,TProperty> context);

#pragma warning disable 1998
		protected virtual async Task<bool> IsValidAsync(PropertyValidatorContext<T,TProperty> context, CancellationToken cancellation) {
			return IsValid(context);
		}
#pragma warning restore 1998

		/// <summary>
		/// Prepares the <see cref="MessageFormatter"/> of <paramref name="context"/> for an upcoming <see cref="ValidationFailure"/>.
		/// </summary>
		/// <param name="context">The validator context</param>
		protected virtual void PrepareMessageFormatterForValidationError(PropertyValidatorContext<T,TProperty> context) {
			context.MessageFormatter.AppendPropertyName(context.DisplayName);
			context.MessageFormatter.AppendPropertyValue(context.PropertyValue);

			// If there's a collection index cached in the root context data then add it
			// to the message formatter. This happens when a child validator is executed
			// as part of a call to RuleForEach. Usually parameters are not flowed through to
			// child validators, but we make an exception for collection indices.
			if (context.ParentContext.RootContextData.TryGetValue("__FV_CollectionIndex", out var index)) {
				// If our property validator has explicitly added a placeholder for the collection index
				// don't overwrite it with the cached version.
				if (!context.MessageFormatter.PlaceholderValues.ContainsKey("CollectionIndex")) {
					context.MessageFormatter.AppendArgument("CollectionIndex", index);
				}
			}
		}

		/// <summary>
		/// Creates an error validation result for this validator.
		/// </summary>
		/// <param name="context">The validator context</param>
		/// <returns>Returns an error validation result.</returns>
		protected virtual ValidationFailure CreateValidationError(PropertyValidatorContext<T,TProperty> context) {
			var messageBuilderContext = new MessageBuilderContext<T,TProperty>(context, this);

			var error = context.Rule.MessageBuilder != null
				? context.Rule.MessageBuilder(messageBuilderContext)
				: messageBuilderContext.GetDefaultMessage();

			var failure = new ValidationFailure(context.PropertyName, error, context.PropertyValue);
#pragma warning disable 618
			failure.FormattedMessageArguments = context.MessageFormatter.AdditionalArguments;
#pragma warning restore 618
			failure.FormattedMessagePlaceholderValues = context.MessageFormatter.PlaceholderValues;
#pragma warning disable 618
			//TODO: Fix error code resolution.
			// failure.ErrorCode = ErrorCodeSource?.GetString(context) ?? ValidatorOptions.Global.ErrorCodeResolver(this);
#pragma warning restore 618

			if (CustomStateProvider != null) {
				failure.CustomState = CustomStateProvider(context);
			}

			if (SeverityProvider != null) {
				failure.Severity = SeverityProvider(context);
			}

			return failure;
		}
	}
}
