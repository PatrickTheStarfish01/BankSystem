﻿namespace BankSystem.Services.Implementations
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AutoMapper;
    using AutoMapper.QueryableExtensions;
    using BankSystem.Models;
    using Data;
    using Interfaces;
    using Microsoft.EntityFrameworkCore;
    using Models.BankAccount;

    public class BankAccountService : BaseService, IBankAccountService
    {
        private readonly IBankAccountUniqueIdHelper uniqueIdHelper;

        public BankAccountService(BankSystemDbContext context, IBankAccountUniqueIdHelper uniqueIdHelper)
            : base(context)
        {
            this.uniqueIdHelper = uniqueIdHelper;
        }

        public async Task<string> CreateAsync(BankAccountCreateServiceModel model)
        {
            if (!this.IsEntityStateValid(model) ||
                !this.Context.Users.Any(u => u.Id == model.UserId))
            {
                return null;
            }

            string generatedUniqueId;

            do
            {
                generatedUniqueId = this.uniqueIdHelper.GenerateAccountUniqueId();
            } while (this.Context.Accounts.Any(a => a.UniqueId == generatedUniqueId));

            if (model.Name == null)
            {
                model.Name = generatedUniqueId;
            }

            var dbModel = Mapper.Map<BankAccount>(model);
            dbModel.UniqueId = generatedUniqueId;

            await this.Context.Accounts.AddAsync(dbModel);
            await this.Context.SaveChangesAsync();

            return dbModel.Id;
        }

        public async Task<decimal> GetUserAccountBalanceAsync(string accountId)
            => await this.Context
                .Accounts
                .Where(b => b.Id == accountId)
                .Select(b => b.Balance)
                .SingleOrDefaultAsync();

        public async Task<string> GetUserAccountUniqueId(string accountId)
            => await this.Context
                .Accounts
                .Where(a => a.Id == accountId)
                .Select(a => a.UniqueId)
                .SingleOrDefaultAsync();

        public async Task<string> GetAccountIdAsync(string uniqueId)
            => await this.Context
                .Accounts
                .Where(a => a.UniqueId == uniqueId)
                .Select(a => a.Id)
                .SingleOrDefaultAsync();

        public async Task<T> GetBankAccountAsync<T>(string id)
            where T : BankAccountBaseServiceModel
            => await this.Context
                .Accounts
                .Where(a => a.Id == id)
                .ProjectTo<T>()
                .SingleOrDefaultAsync();

        public async Task<IEnumerable<T>> GetAllUserAccountsAsync<T>(string userId)
            where T : BankAccountBaseServiceModel
            => await this.Context
                .Accounts
                .Where(a => a.UserId == userId)
                .ProjectTo<T>()
                .ToArrayAsync();
    }
}