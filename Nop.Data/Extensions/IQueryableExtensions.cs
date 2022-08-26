using System.Linq;
using LinqToDB;
using Nop.Core;
using Nop.Core.Infrastructure;

namespace Nop.Data.Extensions
{
    public static class IQueryableExtensions
    {
        public static IQueryable<T> CheckStoreId<T>(this IQueryable<T> query, int storeId)
        {
            //check if store filter enabled and if entity is IMUSTHaveStore
            if (typeof(T).GetInterfaces().Any(x => x.Name == nameof(IMustHaveStore)) && storeId != 0 && (!EngineContext.Current.StoreFilterEnabled.HasValue || EngineContext.Current.StoreFilterEnabled.Value))
                return query.Where(x => (x as IMustHaveStore).StoreId == storeId || (x as IMustHaveStore).StoreId == 0);
            return query;
        }
        public static IQueryable<T> CheckStoreId<T>(this ITable<T> query, int storeId)
        {
            //check if store filter enabled and if entity is IMUSTHaveStore

            if (typeof(T).GetInterfaces().Any(x => x.Name == nameof(IMustHaveStore)) && storeId != 0 && (!EngineContext.Current.StoreFilterEnabled.HasValue || EngineContext.Current.StoreFilterEnabled.Value))
                return query.Where(x => (x as IMustHaveStore).StoreId == storeId || (x as IMustHaveStore).StoreId == 0);
            return query;
        }
    }
}
