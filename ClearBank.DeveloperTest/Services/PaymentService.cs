using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Types;
using System.Configuration;

namespace ClearBank.DeveloperTest.Services
{
    using static AllowedPaymentSchemes;

    public class PaymentService : IPaymentService
    {
        private readonly IAccountDataStore _accountStore;

        public PaymentService(IAccountDataStore accountStore)
        {
            _accountStore = accountStore;
        }

        // I'm using this static method as a lightweight way of creating the same Payment Service as previously.
        // I'd expect that this logic might be moved to a factory or IoC container in the future.
        public static PaymentService Create()
        {
            var dataStoreType = ConfigurationManager.AppSettings["DataStoreType"] == "Backup" ? (IAccountDataStore)
                                    new BackupAccountDataStore() :
                                    new AccountDataStore();

            return new PaymentService(dataStoreType);
        }
        
        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            var account = _accountStore.GetAccount(request.DebtorAccountNumber);
            if (account == null) return new MakePaymentResult();
            
            return ValidateRequest(account, request) ? DebitAccount(request.Amount, account) : new MakePaymentResult();
        }

        private static bool ValidateRequest(Account account, MakePaymentRequest request) 
            => request.PaymentScheme switch
            {
                PaymentScheme.Bacs => account.AllowedPaymentSchemes.HasFlag(Bacs),
                PaymentScheme.FasterPayments => account.AllowedPaymentSchemes.HasFlag(FasterPayments) &&
                                                request.Amount <= account.Balance,
                PaymentScheme.Chaps => account.AllowedPaymentSchemes.HasFlag(Chaps) &&
                                       account.Status == AccountStatus.Live,
            };

        private MakePaymentResult DebitAccount(decimal amount, Account account)
        {
            account.Balance -= amount;
            _accountStore.UpdateAccount(account);
            return new MakePaymentResult { Success = true };
        }
    }
}
