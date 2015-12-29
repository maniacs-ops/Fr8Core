﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Data.Entities;
using Data.Interfaces.DataTransferObjects;
using Data.Interfaces.Manifests;

namespace terminalQuickBooksTests.Fixtures
{
    class HealthMonitor_FixtureData
    {
        public static AuthorizationTokenDTO QuickBooks_AuthTokenDTO()
        {
            return new AuthorizationTokenDTO()
            {
                Token = "lvprdLxfSv3vtByicAac86nlzDiuYnoTA5KxB5roJzCcSpbw;;;;;;;f2TY1CkAtXm1kwXGwcUTCQyiwOAKwJeAvbhLDeFw;;;;;;;1429888620"
            };
        }
        public static AuthorizationTokenDO QuickBooks_AuthTokenDO()
        {
            return new AuthorizationTokenDO()
            {
                Token = "lvprdLxfSv3vtByicAac86nlzDiuYnoTA5KxB5roJzCcSpbw;;;;;;;f2TY1CkAtXm1kwXGwcUTCQyiwOAKwJeAvbhLDeFw;;;;;;;1429888620"
            };
        }

        public static ActivityTemplateDTO Action_Create_Journal_Entry_ActivityTemplate()
        {
            return new ActivityTemplateDTO()
            {
                Id = 1,
                Name = "Create_Journal_Entry_TEST",
                Version = "1"
            };
        }
        public static ActionDTO Action_Create_Journal_Entry_v1_InitialConfiguration_ActionDTO()
        {
            var activityTemplate = Action_Create_Journal_Entry_ActivityTemplate();

            return new ActionDTO()
            {
                Id = Guid.NewGuid(),
                Name = "Create_Journal_Entry",
                Label = "Create Journal Entry",
                AuthToken = QuickBooks_AuthTokenDTO(),
                ActivityTemplate = activityTemplate,
                ActivityTemplateId = activityTemplate.Id
            };
        }
        public static StandardAccountingTransactionCM GetAccountingTransactionCM()
        {
            var curFinLineDTOList = new List<FinancialLineDTO>();
            var curFirstLineDTO = new FinancialLineDTO()
            {
                Amount = "100",
                AccountId = "1",
                AccountName = "Account-A",
                DebitOrCredit = "Debit",
                Description = "Move money to Accout-B"
            };
            var curSecondLineDTO = new FinancialLineDTO()
            {
                Amount = "100",
                AccountId = "2",
                AccountName = "Account-B",
                DebitOrCredit = "Credit",
                Description = "Move money from Accout-A"
            };
            curFinLineDTOList.Add(curFirstLineDTO);
            curFinLineDTOList.Add(curSecondLineDTO);


            var curAccoutingTransactionDTO = new StandardAccountingTransactionDTO()
            {
                Memo = "That is the test crate",
                FinancialLines = curFinLineDTOList,
                Name = "Code1",
                TransactionDate = DateTime.Parse("2015-12-15")
            };
            var curCrate = new StandardAccountingTransactionCM()
            {
                AccountingTransactions = new List<StandardAccountingTransactionDTO>()
                {
                   curAccoutingTransactionDTO
                }
            };
            return curCrate;
        }
    }
}
