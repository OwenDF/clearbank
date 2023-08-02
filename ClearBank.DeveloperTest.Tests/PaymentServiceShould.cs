using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using FluentAssertions;
using Moq;
using Xunit;

namespace ClearBank.DeveloperTest.Tests
{
    using static AccountStatus;
    using static Times;

    public class PaymentServiceShould
    {
        private Mock<IAccountDataStore> _dataStore;

        [Fact]
        public void NotMakePaymentIfDebtorAccountNotFound()
        {
            var result = CreatePaymentService().MakePayment(new MakePaymentRequest());
            
            _dataStore.Verify(x => x.UpdateAccount(It.IsAny<Account>()), Never);
            result.Success.Should().BeFalse();
        }

        [Theory]
        [InlineData(AllowedPaymentSchemes.Bacs, true)]
        [InlineData((AllowedPaymentSchemes) 0, false)]
        public void MakeBacsPaymentIfAllowed(AllowedPaymentSchemes allowedMethods, bool shouldWork)
        {
            const decimal startingBalance = 99.99m;
            const decimal paymentAmount = 34.12m;

            var request = new MakePaymentRequest { DebtorAccountNumber = "One", Amount = paymentAmount, PaymentScheme = PaymentScheme.Bacs };
            var account = new Account { Balance = startingBalance, AllowedPaymentSchemes = allowedMethods };

            var service = CreatePaymentService();
            _dataStore.Setup(x => x.GetAccount("One")).Returns(account);

            var result = service.MakePayment(request);
            _dataStore.Verify(x => x.UpdateAccount(It.Is<Account>(a => a.Balance == startingBalance - paymentAmount)), Exactly(shouldWork ? 1 : 0));

            result.Success.Should().Be(shouldWork);
        }

        [Theory]
        [InlineData(AllowedPaymentSchemes.FasterPayments, 34.12, true)]
        [InlineData(AllowedPaymentSchemes.FasterPayments, 10000.00, true)]
        [InlineData(AllowedPaymentSchemes.FasterPayments, 34.11, false)]
        [InlineData((AllowedPaymentSchemes) 0, 10000.00d, false)]
        public void MakeFasterPaymentIfAllowed(AllowedPaymentSchemes allowedMethods, double startingBalanceD, bool shouldWork)
        {
            // Decimals aren't a core CLR type so can't use as attribute arguments...
            var startingBalance = (decimal) startingBalanceD;
            const decimal paymentAmount = 34.12m;

            var request = new MakePaymentRequest { DebtorAccountNumber = "One", Amount = paymentAmount, PaymentScheme = PaymentScheme.FasterPayments };
            var account = new Account { Balance = startingBalance, AllowedPaymentSchemes = allowedMethods };

            var service = CreatePaymentService();
            _dataStore.Setup(x => x.GetAccount("One")).Returns(account);

            var result = service.MakePayment(request);
            _dataStore.Verify(x => x.UpdateAccount(It.Is<Account>(a => a.Balance == startingBalance - paymentAmount)), Exactly(shouldWork ? 1 : 0));

            result.Success.Should().Be(shouldWork);
        }

        
        [Theory]
        [InlineData(AllowedPaymentSchemes.Chaps, Live, true)]
        [InlineData(AllowedPaymentSchemes.Chaps, Disabled, false)]
        [InlineData(AllowedPaymentSchemes.Chaps, InboundPaymentsOnly, false)]
        [InlineData((AllowedPaymentSchemes) 0, Live, false)]
        public void MakeChapsPaymentIfAllowed(AllowedPaymentSchemes allowedMethods, AccountStatus accountStatus, bool shouldWork)
        {
            const decimal startingBalance = 99.99m;
            const decimal paymentAmount = 34.12m;

            var request = new MakePaymentRequest { DebtorAccountNumber = "One", Amount = paymentAmount, PaymentScheme = PaymentScheme.Chaps };
            var account = new Account { Balance = startingBalance, AllowedPaymentSchemes = allowedMethods, Status = accountStatus };

            var service = CreatePaymentService();
            _dataStore.Setup(x => x.GetAccount("One")).Returns(account);

            var result = service.MakePayment(request);
            _dataStore.Verify(x => x.UpdateAccount(It.Is<Account>(a => a.Balance == startingBalance - paymentAmount)), Exactly(shouldWork ? 1 : 0));

            result.Success.Should().Be(shouldWork);
        }

        private IPaymentService CreatePaymentService()
        {
            _dataStore = new Mock<IAccountDataStore>();
            return new PaymentService(_dataStore.Object);
        }
    }
}