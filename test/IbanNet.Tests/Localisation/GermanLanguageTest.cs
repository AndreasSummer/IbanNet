using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IbanNet.Registry;
using IbanNet.Validation.Results;
using IbanNet.Validation.Rules;
using Xunit;

namespace IbanNet.Localisation
{

    public class GermanLanguageTest
    {
        [Fact]
        public void GermanLanguageTestInvalidLenght()
        {
            System.Globalization.CultureInfo before = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
                    new System.Globalization.CultureInfo("de");


                IsValidLengthRule r = new IsValidLengthRule();
                var result = r.Validate(new ValidationRuleContext("AT1234")
                {
                    Country = new IbanCountry("AT")
                });

                var resultTyped = result as InvalidLengthResult;
                if (resultTyped != null)
                {
                    Assert.Equal("Der IBAN hat eine falsche Länge.", resultTyped.ErrorMessage);
                }



            }

            finally
            {
                System.Threading.Thread.CurrentThread.CurrentUICulture = before;
            }


        }

    }
}
