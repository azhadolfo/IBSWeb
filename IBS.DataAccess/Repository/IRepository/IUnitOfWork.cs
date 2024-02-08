﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBS.DataAccess.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICustomerRepository Customer { get; }

        ICategoryRepository Category { get; }

        IProductRepository Product { get; }

        Task SaveAsync();
    }
}