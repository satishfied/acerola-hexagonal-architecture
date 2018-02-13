﻿namespace Acerola.Application.Commands.Withdraw
{
    using System.Threading.Tasks;
    using Acerola.Application.Results;
    using Acerola.Domain.Customers;
    using Acerola.Domain.Customers.Accounts;
    using Acerola.Domain.ValueObjects;

    public class WithdrawHandler : IWithdrawHandler
    {
        private readonly ICustomerReadOnlyRepository customerReadOnlyRepository;
        private readonly ICustomerWriteOnlyRepository customerWriteOnlyRepository;
        private readonly IResultConverter resultConverter;

        public WithdrawHandler(
            ICustomerReadOnlyRepository customerReadOnlyRepository,
            ICustomerWriteOnlyRepository customerWriteOnlyRepository,
            IResultConverter resultConverter)
        {
            this.customerReadOnlyRepository = customerReadOnlyRepository;
            this.customerWriteOnlyRepository = customerWriteOnlyRepository;
            this.resultConverter = resultConverter;
        }

        public async Task<WithdrawResult> Handle(WithdrawCommand command)
        {
            Customer customer = await customerReadOnlyRepository.GetByAccount(command.AccountId);
            if (customer == null)
                throw new AccountNotFoundException($"The account {command.AccountId} does not exists or is already closed.");

            Debit debit = new Debit(new Amount(command.Amount));
            Account account = customer.FindAccount(command.AccountId);
            account.Withdraw(debit);

            await customerWriteOnlyRepository.Update(customer);

            TransactionResult transactionResult = resultConverter.Map<TransactionResult>(debit);
            WithdrawResult response = new WithdrawResult(
                transactionResult,
                account.CurrentBalance.Value
            );

            return response;
        }
    }
}