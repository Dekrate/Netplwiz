using Microsoft.VisualStudio.TestTools.UnitTesting;
using Netplwiz.Services;
using System;

namespace Netplwiz.Tests
{
    [TestClass]
    public class SecurityTests
    {
        private UserService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _service = new UserService();
        }

        [DataTestMethod]
        [DataRow("Administrator")]
        [DataRow("administrator")]
        [DataRow("ADMINISTRATOR")]
        [DataRow("Guest")]
        [DataRow("DefaultAccount")]
        [DataRow("WDAGUtilityAccount")]
        public void IsProtectedAccount_BlocksBuiltInAccounts(string userName)
        {
            Assert.IsTrue(_service.IsProtectedAccount(userName));
        }

        [TestMethod]
        public void IsProtectedAccount_BlocksCurrentUser()
        {
            var current = Environment.UserName;
            Assert.IsTrue(_service.IsProtectedAccount(current));
        }

        [TestMethod]
        public void IsProtectedAccount_AllowsRegularUser()
        {
            Assert.IsFalse(_service.IsProtectedAccount("RegularUser123"));
        }

        [TestMethod]
        public void IsProtectedAccount_Empty_IsProtected()
        {
            Assert.IsTrue(_service.IsProtectedAccount(""));
            Assert.IsTrue(_service.IsProtectedAccount("   "));
            Assert.IsTrue(_service.IsProtectedAccount(null!));
        }

        [TestMethod]
        public void DeleteUser_ProtectedAccount_ReturnsFalse()
        {
            var result = _service.DeleteUser("Administrator");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetPassword_ProtectedAccount_ReturnsFalse()
        {
            var result = _service.SetPassword("Administrator", "NewPass123!");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void SetPassword_EmptyPassword_ReturnsFalse()
        {
            var result = _service.SetPassword("RegularUser", "");
            Assert.IsFalse(result);
        }
    }
}
