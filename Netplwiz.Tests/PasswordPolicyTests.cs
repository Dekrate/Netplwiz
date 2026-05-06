using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netplwiz.Models;

namespace Netplwiz.Tests
{
    [TestClass]
    public class PasswordPolicyTests
    {
        [TestMethod]
        public void IsPasswordValid_EmptyPassword_ReturnsFalse()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 0, PasswordComplexityRequired = false };
            bool result = policy.IsPasswordValid("", out string? error);
            Assert.IsFalse(result);
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public void IsPasswordValid_TooShort_ReturnsFalse()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 8, PasswordComplexityRequired = false };
            bool result = policy.IsPasswordValid("short", out string? error);
            Assert.IsFalse(result);
            StringAssert.Contains(error, "8");
        }

        [TestMethod]
        public void IsPasswordValid_MinLengthMet_ReturnsTrue()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 6, PasswordComplexityRequired = false };
            bool result = policy.IsPasswordValid("abcdef", out string? error);
            Assert.IsTrue(result);
            Assert.IsNull(error);
        }

        [TestMethod]
        public void IsPasswordValid_ComplexityMissing_ReturnsFalse()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 0, PasswordComplexityRequired = true };
            bool result = policy.IsPasswordValid("lowercase", out string? error);
            Assert.IsFalse(result);
            Assert.IsNotNull(error);
        }

        [TestMethod]
        public void IsPasswordValid_ComplexityThreeCategories_ReturnsTrue()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 0, PasswordComplexityRequired = true };
            bool result = policy.IsPasswordValid("Pass1", out string? error);
            Assert.IsTrue(result);
            Assert.IsNull(error);
        }

        [TestMethod]
        public void IsPasswordValid_ComplexityTwoCategories_ReturnsFalse()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 0, PasswordComplexityRequired = true };
            bool result = policy.IsPasswordValid("Password", out string? error);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void IsPasswordValid_ComplexityFourCategories_ReturnsTrue()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 0, PasswordComplexityRequired = true };
            bool result = policy.IsPasswordValid("Pass1!", out string? error);
            Assert.IsTrue(result);
            Assert.IsNull(error);
        }

        [TestMethod]
        public void GetPolicyDescription_NoRestrictions_ReturnsNoRestrictions()
        {
            var policy = new PasswordPolicy();
            string desc = policy.GetPolicyDescription();
            Assert.AreEqual("Brak ograniczeń", desc);
        }

        [TestMethod]
        public void GetPolicyDescription_MinLengthAndComplexity_ReturnsExpected()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 8, PasswordComplexityRequired = true };
            string desc = policy.GetPolicyDescription();
            StringAssert.Contains(desc, "min. 8 znaków");
            StringAssert.Contains(desc, "wymagana złożoność");
        }

        [TestMethod]
        public void GetPolicyDescription_Lockout_ReturnsExpected()
        {
            var policy = new PasswordPolicy { AccountLockoutThreshold = 5 };
            string desc = policy.GetPolicyDescription();
            StringAssert.Contains(desc, "blokada po 5 próbach");
        }

        [TestMethod]
        public void IsPasswordValid_NullPassword_ReturnsFalse()
        {
            var policy = new PasswordPolicy { MinimumPasswordLength = 0, PasswordComplexityRequired = false };
            bool result = policy.IsPasswordValid(null!, out string? error);
            Assert.IsFalse(result);
            Assert.IsNotNull(error);
        }
    }
}
