using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Netplwiz.Models;
using Netplwiz.Services;
using Netplwiz.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Netplwiz.Tests
{
    /// <summary>
    /// End-to-end tests that exercise the full ViewModel -> Service command flow
    /// without touching the actual Windows user directory or registry.
    /// Every UI "button" (command) is pressed and verified against a mock service.
    /// </summary>
    [TestClass]
    public class EndToEndCommandTests
    {
        private Mock<IUserService> _mock = null!;
        private MainViewModel _vm = null!;

        [TestInitialize]
        public void Setup()
        {
            _mock = new Mock<IUserService>();
            _mock.Setup(s => s.IsSecureLogonRequired()).Returns(false);
            _vm = new MainViewModel(_mock.Object);
        }

        [TestMethod]
        public async Task E2E_FullFlow_LoadUsers_PressesAllUserButtons()
        {
            // Arrange: simulate two users returned by the system
            var users = new List<UserAccount>
            {
                new() { UserName = "Alice", FullName = "Alice Smith" },
                new() { UserName = "Bob", FullName = "Bob Jones" }
            };
            _mock.Setup(s => s.GetLocalUsers()).Returns(users);

            // Act: open app -> users load automatically in ctor, but we await here for determinism
            await _vm.LoadUsersAsync();

            // Assert: both users visible
            Assert.AreEqual(2, _vm.Users.Count);

            // Act: select first user -> "Properties" button becomes enabled
            _vm.SelectedUser = _vm.Users[0];
            Assert.IsTrue(_vm.PropertiesCommand.CanExecute(null));

            // Act: press "Properties" -> event should fire
            var propertiesFired = false;
            _vm.RequestUserProperties += (_, _) => propertiesFired = true;
            _vm.PropertiesCommand.Execute(null);
            Assert.IsTrue(propertiesFired);

            // Act: press "Reset Password" -> event should fire
            var resetFired = false;
            _vm.RequestResetPassword += (_, _) => resetFired = true;
            _vm.ResetPasswordCommand.Execute(null);
            Assert.IsTrue(resetFired);

            // Act: press "Remove" -> event should fire
            var removeFired = false;
            _vm.RequestRemoveUser += (_, _) => removeFired = true;
            _vm.RemoveUserCommand.Execute(null);
            Assert.IsTrue(removeFired);

            // Act: deselect user -> all user-action buttons disabled
            _vm.SelectedUser = null;
            Assert.IsFalse(_vm.RemoveUserCommand.CanExecute(null));
            Assert.IsFalse(_vm.PropertiesCommand.CanExecute(null));
            Assert.IsFalse(_vm.ResetPasswordCommand.CanExecute(null));
        }

        [TestMethod]
        public void E2E_AdvancedTab_PressesAllButtons()
        {
            // Act: press "Manage Passwords"
            _vm.ManagePasswordsCommand.Execute(null);
            _mock.Verify(s => s.OpenCredentialManager(), Times.Once);

            // Act: press "Advanced User Management"
            _vm.AdvancedUserManagementCommand.Execute(null);
            _mock.Verify(s => s.OpenAdvancedUserManagement(), Times.Once);

            // Act: toggle secure logon on
            _vm.SecureLogonRequired = true;
            _mock.Verify(s => s.SetSecureLogonRequired(true), Times.Once);

            // Act: toggle back
            _vm.SecureLogonRequired = false;
            _mock.Verify(s => s.SetSecureLogonRequired(false), Times.Once);
        }

        [TestMethod]
        public async Task E2E_PropertiesDialog_SaveFlow()
        {
            var user = new UserAccount { UserName = "Alice", FullName = "Alice Smith", Description = "Dev" };
            _mock.Setup(s => s.UpdateUser(It.IsAny<UserAccount>())).Returns(true);

            var pvm = new UserPropertiesViewModel(user, _mock.Object)
            {
                FullName = "Alice Smith-Jones",
                Description = "Senior Dev"
            };

            await pvm.SaveAsync();

            Assert.AreEqual("Zapisano pomyślnie", pvm.StatusMessage);
            _mock.Verify(s => s.UpdateUser(It.Is<UserAccount>(u => u.FullName == "Alice Smith-Jones")), Times.Once);
        }

        [TestMethod]
        public void E2E_DeleteUser_ProtectedAccount_Blocked()
        {
            _mock.Setup(s => s.DeleteUser(It.IsAny<string>())).Returns(true);
            _mock.Setup(s => s.IsProtectedAccount("Administrator")).Returns(true);

            // Even if the mock says DeleteUser would succeed, the real UserService blocks it.
            // Here we test the real service security layer directly.
            var realService = new UserService();
            var result = realService.DeleteUser("Administrator");
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void E2E_SetPassword_ProtectedAccount_Blocked()
        {
            var realService = new UserService();
            var result = realService.SetPassword("Guest", "AnyPass123!");
            Assert.IsFalse(result);
        }
    }
}
