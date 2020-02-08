﻿using System;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using IbanNet.Validation.Results;
using Moq;
using NUnit.Framework;

namespace IbanNet
{
	[TestFixture]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	internal class IbanTests
		: IbanTestFixture
	{
		public class When_parsing_iban : IbanTests
		{
			[Test]
			public void With_null_value_should_throw()
			{
				// Act
				Action act = () => Iban.Parse(null);

				// Assert
				act.Should().Throw<ArgumentNullException>("the provided value was null").Which.ParamName.Should().Be("value");
			}

			[Test]
			public void With_invalid_value_should_throw()
			{
				// Act
				Action act = () => Iban.Parse(TestValues.InvalidIban);

				// Assert
				IbanFormatException ex = act.Should().Throw<IbanFormatException>("the provided value was invalid").Which;
				ex.Result.Should().BeEquivalentTo(new ValidationResult
				{
					Error = new IllegalCharactersResult(),
					AttemptedValue = TestValues.InvalidIban
				});
				ex.InnerException.Should().BeNull();
				ex.Message.Should().Be("The IBAN contains illegal characters.");
			}

			[Test]
			public void With_valid_value_should_return_iban()
			{
				Iban iban = null;

				// Act
				Action act = () => iban = Iban.Parse(TestValues.ValidIban);

				// Assert
				act.Should().NotThrow<IbanFormatException>();
				iban.Should().NotBeNull("the value should be parsed")
					.And.BeOfType<Iban>()
					.Which.ToString()
					.Should().Be(TestValues.ValidIban, "the returned value should match the provided value");
			}

			[Test]
			public void With_value_that_fails_custom_rule_should_throw()
			{
				// Act
				Action act = () => Iban.Parse(TestValues.IbanForCustomRuleFailure);

				// Assert
				IbanFormatException ex = act.Should().Throw<IbanFormatException>("the provided value was invalid").Which;
				ex.Result.Should().BeEquivalentTo(new ValidationResult
				{
					Error = new ErrorResult("Custom message"),
					AttemptedValue = TestValues.IbanForCustomRuleFailure
				});
				ex.InnerException.Should().BeNull();
				ex.Message.Should().Be("Custom message");
			}

			[Test]
			public void With_value_that_causes_custom_rule_to_throw_should_rethrow()
			{
				// Act
				Action act = () => Iban.Parse(TestValues.IbanForCustomRuleException);

				// Assert
				IbanFormatException ex = act.Should().Throw<IbanFormatException>("the provided value was invalid").Which;
				ex.Result.Should().BeNull();
				ex.InnerException.Should().NotBeNull();
				ex.Message.Should().Contain("is not a valid IBAN.");
			}
		}

		public class When_trying_to_parse_iban : IbanTests
		{
			[Test]
			public void With_null_value_should_return_false()
			{
				// Act
				bool actual = Iban.TryParse(null, out Iban iban);

				// Assert
				actual.Should().BeFalse("the provided value was null which is not valid");
				iban.Should().BeNull("parsing did not succeed");

			}

			[Test]
			public void With_invalid_value_should_return_false()
			{
				// Act
				bool actual = Iban.TryParse(TestValues.InvalidIban, out Iban iban);

				// Assert
				actual.Should().BeFalse("the provided value was invalid");
				iban.Should().BeNull("parsing did not succeed");

				IbanValidatorMock.Verify(m => m.Validate(It.IsAny<string>()), Times.Once);
			}

			[Test]
			public void With_valid_value_should_pass()
			{
				// Act
				bool actual = Iban.TryParse(TestValues.ValidIban, out Iban iban);

				// Assert
				actual.Should().BeTrue("the provided value was valid");
				iban.Should().NotBeNull().
					And.BeOfType<Iban>()
					.Which.ToString()
					.Should().Be(TestValues.ValidIban);

				IbanValidatorMock.Verify(m => m.Validate(It.IsAny<string>()), Times.Once);
			}
		}

		public class When_formatting : IbanTests
		{
			private Iban _iban;

			public override void SetUp()
			{
				base.SetUp();

				_iban = new Iban(TestValues.ValidIban);
			}

			[Test]
			public void With_null_format_should_throw()
			{
				// Act
				Action act = () => _iban.ToString(null);

				// Assert
				act.Should().Throw<ArgumentNullException>("the provided format was null")
					.Which.ParamName.Should().Be("format");
			}

			[TestCase("f")]
			[TestCase("s")]
			[TestCase("invalid_format")]
			[TestCase("")]
			[TestCase(null)]
			public void With_invalid_format_should_throw(string format)
			{
				// Act
				Action act = () => _iban.ToString(format);

				// Assert
				act.Should().Throw<ArgumentException>("the provided format was invalid")
					.Which.ParamName.Should().Be("format");
			}

			[TestCase(Iban.Formats.Flat, TestValues.ValidIban)]
			[TestCase(Iban.Formats.Partitioned, TestValues.ValidIbanPartitioned)]
			public void With_valid_format_should_succeed(string format, string expected)
			{
				// Act
				string actual = _iban.ToString(format);

				// Assert
				actual.Should().Be(expected);
			}

			[Test]
			public void With_default_format_should_return_flat()
			{
				// Act
				string actual = _iban.ToString();

				// Assert
				actual.Should().Be(TestValues.ValidIban);
			}
		}

		public class When_comparing_for_equality : IbanTests
		{
			private Iban _iban;
			private Iban _equalIban;
			private Iban _otherIban;

			public override void SetUp()
			{
				base.SetUp();

				var ibanParser = new IbanParser(IbanValidatorMock.Object);

				_iban = ibanParser.Parse(TestValues.ValidIban);
				_equalIban = ibanParser.Parse(TestValues.ValidIbanPartitioned);
				_otherIban = ibanParser.Parse("AE070331234567890123456");
			}

			[Test]
			public void When_values_are_equal_should_return_true()
			{
				// Act
				bool actual = _iban == _equalIban;

				// Assert
				actual.Should().BeTrue();
			}

			[Test]
			public void By_reference_when_values_are_equal_should_return_true()
			{
				// Act
				bool actual = _iban.Equals(_equalIban);

				// Assert
				actual.Should().BeTrue();
			}

			[Test]
			public void By_reference_when_other_is_null_should_return_false()
			{
				Iban nullIban = null;

				// Act
				// ReSharper disable once ExpressionIsAlwaysNull
				bool actual = _iban.Equals(nullIban);

				// Assert
				actual.Should().BeFalse();
			}

			[Test]
			public void By_reference_when_other_is_self_should_return_true()
			{
				// Act
				// ReSharper disable once EqualExpressionComparison
				bool actual = _iban.Equals(_iban);

				// Assert
				actual.Should().BeTrue();
			}

			[Test]
			public void By_reference_when_other_is_wrong_type_should_return_false()
			{
				var otherType = new object();

				// Act
				bool actual = _iban.Equals(otherType);

				// Assert
				actual.Should().BeFalse();
			}

			[Test]
			public void When_values_are_not_equal_should_return_false()
			{
				// Act
				bool actual = _iban == _otherIban;

				// Assert
				actual.Should().BeFalse();
			}
		}

		public class When_comparing_for_inequality : IbanTests
		{
			private Iban _iban;
			private Iban _equalIban;
			private Iban _otherIban;

			public override void SetUp()
			{
				base.SetUp();

				var ibanParser = new IbanParser(IbanValidatorMock.Object);

				_iban = ibanParser.Parse(TestValues.ValidIban);
				_equalIban = ibanParser.Parse(TestValues.ValidIbanPartitioned);
				_otherIban = ibanParser.Parse("AE070331234567890123456");
			}

			[Test]
			public void When_values_are_not_equal_should_return_true()
			{
				// Act
				bool actual = _iban != _otherIban;

				// Assert
				actual.Should().BeTrue();
			}

			[Test]
			public void By_reference_when_values_are_not_equal_should_return_false()
			{
				// Act
				bool actual = _iban.Equals(_otherIban);

				// Assert
				actual.Should().BeFalse();
			}

			[Test]
			public void When_values_are_equal_should_return_false()
			{
				// Act
				bool actual = _iban != _equalIban;

				// Assert
				actual.Should().BeFalse();
			}
		}

		public class When_getting_hashcode : IbanTests
		{
			[Test]
			public void It_should_be_same_as_underlying_string_value()
			{
				var iban = new Iban(TestValues.ValidIban);
				int expectedHashCode = TestValues.ValidIban.GetHashCode();

				// Act
				int actual = iban.GetHashCode();

				// Assert
				actual.Should().Be(expectedHashCode);
			}
		}
	}
}
