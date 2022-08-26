using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Stores;
using Nop.Core.Infrastructure;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Stores;
using Nop.Services.Tasks;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Web.Framework
{
    /// <summary>
    /// Store context for web application
    /// </summary>
    public partial class WebStoreContext : IStoreContext
    {
        #region Fields

        //private readonly IGenericAttributeService _genericAttributeService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;

        private Store _cachedStore;
        private Customer _cachedCustomer;
        private int? _cachedActiveStoreScopeConfiguration;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="genericAttributeService">Generic attribute service</param>
        /// <param name="httpContextAccessor">HTTP context accessor</param>
        /// <param name="storeService">Store service</param>
        public WebStoreContext(
            IHttpContextAccessor httpContextAccessor,
            IStoreService storeService,
            ISettingService settingService)
        {
            _settingService = settingService;
            _httpContextAccessor = httpContextAccessor;
            _storeService = storeService;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current store
        /// </summary>
        public virtual Store CurrentStore
        {
            get
            {

                if (_cachedStore != null && _cachedStore.Id != 0)
                    return _cachedStore;

                //try to determine the current store by HOST header
                string host = _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.Host];
                var userAgent = _httpContextAccessor.HttpContext?.Request?.Headers[HeaderNames.UserAgent];
                var isRegisterRequest = _httpContextAccessor.HttpContext?.Request?.Path.Value.Contains(NopPathRouteDefaults.RegistrationEndPointName);
                var store = new Store();
                if (isRegisterRequest.HasValue && isRegisterRequest.Value)
                {
                    var requestBody = _httpContextAccessor.HttpContext?.Request.ReadFormAsync().Result;
                    if (requestBody != null)
                    {
                        var storeId = requestBody["storeId"].FirstOrDefault();
                        if (storeId != null)
                            store = _storeService.GetStoreById(Int32.Parse(storeId));
                    }

                }
                else if (userAgent.HasValue && (userAgent.Value.Any(x => x.Contains("EXA-IOS")) || userAgent.Value.Any(x => x.Contains("EXA-Android"))) && (!isRegisterRequest.HasValue || !isRegisterRequest.Value))
                    store = GetStoreForMobile();
                else
                    store = GetStoreForWeb(host);
                if (string.IsNullOrEmpty(host))
                {
                    var setting = _settingService.LoadSetting<CommonSettings>();
                    if (setting != null)
                    {
                        store.Url = setting.DefaultUrl;
                    }
                }
                EngineContext.Current.CurrentStoreId = store.Id;
                _cachedStore = store ?? throw new Exception("No store could be loaded");

                return _cachedStore;
            }
        }

        /// <summary>
        /// Gets active store scope configuration
        /// </summary>
        public virtual int ActiveStoreScopeConfiguration
        {
            get
            {
                if (_cachedActiveStoreScopeConfiguration.HasValue)
                    return _cachedActiveStoreScopeConfiguration.Value;

                //ensure that we have 2 (or more) stores
                if (_storeService.GetAllStores().Count > 1)
                {
                    //do not inject IWorkContext via constructor because it'll cause circular references
                    var currentCustomer = EngineContext.Current.Resolve<IWorkContext>().CurrentCustomer;

                    //try to get store identifier from attributes
                    //var storeId = _genericAttributeService
                    //    .GetAttribute<int>(currentCustomer, NopCustomerDefaults.AdminAreaStoreScopeConfigurationAttribute);
                    _cachedActiveStoreScopeConfiguration = EngineContext.Current.CurrentStoreId;
                }
                else
                    _cachedActiveStoreScopeConfiguration = 0;

                return _cachedActiveStoreScopeConfiguration ?? 0;
            }
        }
        public virtual Customer CurrentCustomerForMobile
        {
            get
            {
                if (_cachedCustomer != null)
                    return _cachedCustomer;

                Customer customer = null;
                var _customerRepository = EngineContext.Current.Resolve<IRepository<Customer>>();

                if (customer == null || customer.Deleted || !customer.Active || customer.RequireReLogin)
                {
                    //try to get registered user
                    if (_httpContextAccessor.HttpContext != null && !_httpContextAccessor.HttpContext.Request.Path.Equals(new PathString($"/{NopTaskDefaults.ScheduleTaskPath}"), StringComparison.InvariantCultureIgnoreCase))
                    {
                        var claims = _httpContextAccessor.HttpContext.User;
                        if (claims != null)
                        {
                            var usernameClaim = claims.FindFirst(claim => claim.Type == ClaimTypes.Name);
                            if (usernameClaim != null)
                                customer = _customerRepository.Table.Where(x => x.Username == usernameClaim.Value && !x.Deleted).FirstOrDefault();
                        }
                    }
                }




                _cachedCustomer = customer;
                return customer;
            }
        }

        #endregion
        #region Utilities
        private Store GetStoreForMobile()
        {
            var customer = CurrentCustomerForMobile;
            var storeId = customer?.StoreId == null ? 0 : customer.StoreId;
            var store = _storeService.GetStoreById(storeId);
            return store ?? new Store();
        }
        private Store GetStoreForWeb(string host)
        {
            var allStores = _storeService.GetAllStores();
            var store = allStores.FirstOrDefault(s => _storeService.ContainsHostValue(s, host));
            return store ?? new Store();
        }
        #endregion
    }
}