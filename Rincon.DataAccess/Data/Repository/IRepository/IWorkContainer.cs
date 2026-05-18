using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rincon.DataAccess.Data.Repository.IRepository
{
    public interface IWorkContainer : IDisposable
    {
        ICategoryRepository Category { get; }
        void Save();
    }
}
