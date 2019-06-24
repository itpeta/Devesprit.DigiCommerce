﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Devesprit.Core.Localization;
using Devesprit.Core.Settings;
using Devesprit.Data;
using Devesprit.Data.Domain;
using Z.EntityFramework.Plus;

namespace Devesprit.Services.Localization 
{
    public partial class LocalizedEntityService : ILocalizedEntityService
    {
        private readonly AppDbContext _dbContext;

        public LocalizedEntityService(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        protected virtual async Task DeleteLocalizedPropertyAsync(TblLocalizedProperty localizedProperty)
        {
            if (localizedProperty == null)
                throw new ArgumentNullException(nameof(localizedProperty));

            await _dbContext.LocalizedProperty.Where(p=> p.Id == localizedProperty.Id).DeleteAsync();
            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        protected virtual void DeleteLocalizedProperty(TblLocalizedProperty localizedProperty)
        {
            if (localizedProperty == null)
                throw new ArgumentNullException(nameof(localizedProperty));

            _dbContext.LocalizedProperty.Where(p => p.Id == localizedProperty.Id).Delete();
            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        public virtual TblLocalizedProperty FindById(int localizedPropertyId)
        {
            if (localizedPropertyId == 0)
                return null;

            return _dbContext.LocalizedProperty.DeferredFirstOrDefault(p => p.Id == localizedPropertyId)
                .FromCache(QueryCacheTag.LocalizedProperty);
        }

        public virtual string GetLocalizedString(int languageId, int entityId, string localeKeyGroup, string localeKey)
        {
            var tblLocalizedProperty = _dbContext.LocalizedProperty
                .DeferredFirstOrDefault(p => p.LanguageId == languageId && p.EntityId == entityId &&
                            p.LocaleKeyGroup == localeKeyGroup && p.LocaleKey == localeKey)
                .FromCache(QueryCacheTag.LocalizedProperty);
            if (tblLocalizedProperty != null)
                return tblLocalizedProperty.LocaleValue;
            return string.Empty;
        }

        protected virtual async Task InsertLocalizedPropertyAsync(TblLocalizedProperty localizedProperty)
        {
            if (localizedProperty == null)
                throw new ArgumentNullException(nameof(localizedProperty));
            _dbContext.LocalizedProperty.Add(localizedProperty);

            await _dbContext.SaveChangesAsync();
            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        protected virtual async Task UpdateLocalizedPropertyAsync(TblLocalizedProperty localizedProperty)
        {
            if (localizedProperty == null)
                throw new ArgumentNullException(nameof(localizedProperty));
            await _dbContext.LocalizedProperty.Where(p=> p.Id == localizedProperty.Id).UpdateAsync(p => new TblLocalizedProperty()
            {
                EntityId = localizedProperty.EntityId,
                LanguageId = localizedProperty.LanguageId,
                LocaleKey = localizedProperty.LocaleKey,
                LocaleKeyGroup = localizedProperty.LocaleKeyGroup,
                LocaleValue = localizedProperty.LocaleValue
            });

            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        public virtual async Task DeleteEntityAllLocalizedStringsAsync<T>(T entity, int? languageId = null) where T : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            await DeleteEntityAllLocalizedStringsAsync(typeof(T).Name, entity.Id, languageId);
        }

        protected virtual void InsertLocalizedProperty(TblLocalizedProperty localizedProperty)
        {
            if (localizedProperty == null)
                throw new ArgumentNullException(nameof(localizedProperty));
            _dbContext.LocalizedProperty.Add(localizedProperty);

            _dbContext.SaveChanges();
            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        protected virtual void UpdateLocalizedProperty(TblLocalizedProperty localizedProperty)
        {
            if (localizedProperty == null)
                throw new ArgumentNullException(nameof(localizedProperty));
            _dbContext.LocalizedProperty.Where(p=> p.Id == localizedProperty.Id).Update(p => new TblLocalizedProperty()
            {
                EntityId = localizedProperty.EntityId,
                LanguageId = localizedProperty.LanguageId,
                LocaleKey = localizedProperty.LocaleKey,
                LocaleKeyGroup = localizedProperty.LocaleKeyGroup,
                LocaleValue = localizedProperty.LocaleValue
            });

            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        public virtual void DeleteEntityAllLocalizedStrings<T>(T entity, int? languageId = null) where T : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            DeleteEntityAllLocalizedStrings(typeof(T).Name, entity.Id, languageId);
        }

        public virtual async Task DeleteEntityAllLocalizedStringsAsync(string localeKeyGroup, int entityId, int? languageId = null)
        {
            var query = _dbContext.LocalizedProperty.Where(p =>
                p.EntityId == entityId && p.LocaleKeyGroup == localeKeyGroup);
            if (languageId != null)
            {
                query = query.Where(p => p.LanguageId == languageId);
            }

            await query.DeleteAsync();

            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        public virtual void DeleteEntityAllLocalizedStrings(string localeKeyGroup, int entityId, int? languageId = null)
        {
            var query = _dbContext.LocalizedProperty.Where(p =>
                p.EntityId == entityId && p.LocaleKeyGroup == localeKeyGroup);
            if (languageId != null)
            {
                query = query.Where(p => p.LanguageId == languageId);
            }

            query.Delete();

            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        public virtual void DeleteEntityAllLocalizedStrings(int entityId, string localeKeyGroup, string localKey, int? languageId = null)
        {
            var query = _dbContext.LocalizedProperty.Where(p =>
                p.EntityId == entityId && p.LocaleKeyGroup == localeKeyGroup && p.LocaleKey == localKey);
            if (languageId != null)
            {
                query = query.Where(p => p.LanguageId == languageId);
            }

            query.Delete();

            QueryCacheManager.ExpireTag(QueryCacheTag.LocalizedProperty);
        }

        public virtual async Task SaveLocalizedStringAsync<T>(T entity, Expression<Func<T, string>> keySelector, string localeValue,
            int languageId) where T : BaseEntity
        {
            await SaveLocalizedStringAsync<T, string>(entity, keySelector, localeValue, languageId);
        }

        public virtual async Task SaveAllLocalizedStringsAsync<T>(T entity, object model) where T : BaseEntity
        {
            foreach (var property in model.GetType().GetProperties().Where(p=> p.PropertyType == typeof(LocalizedString)))
            {
                var localizedString = (LocalizedString) property.GetValue(model);
                var propertyName = property.Name;
                foreach (var item in localizedString.Where(p => p.Key != 0))
                {
                    await SaveLocalizedStringAsync(entity, propertyName, typeof(T).Name, item.Value, item.Key);
                }
            }
        }

        public virtual async Task SaveLocalizedStringAsync<T>(T entity, Expression<Func<T, string>> keySelector, LocalizedString localizedString) where T : BaseEntity
        {
            foreach (var item in localizedString.Where(p=> p.Key != 0))
            {
                await SaveLocalizedStringAsync<T, string>(entity, keySelector, item.Value, item.Key);
            }
        }

        public virtual async Task SaveLocalizedStringAsync<T, TPropType>(T entity, Expression<Func<T, TPropType>> keySelector,
            TPropType localeValue, int languageId) where T : BaseEntity
        {
            if (!(keySelector.Body is MemberExpression member))
            {
                throw new ArgumentException($"Expression '{keySelector}' refers to a method, not a property.");
            }

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException($"Expression '{keySelector}' refers to a field, not a property.");
            }

            string localeKeyGroup = typeof(T).Name;
            string localeKey = propInfo.Name;

            await SaveLocalizedStringAsync(entity, localeKey, localeKeyGroup, localeValue, languageId);
        }

        public virtual async Task SaveLocalizedStringAsync<T, TPropType>(T entity, string localeKey, string localeKeyGroup,
            TPropType localeValue, int languageId) where T : BaseEntity
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            if (languageId == 0)
                throw new ArgumentOutOfRangeException(nameof(languageId), @"Language ID should not be 0");

            var props = GetLocalizedProperties(entity.Id, localeKeyGroup);
            var prop = props.FirstOrDefault(lp => lp.LanguageId == languageId &&
                                                  lp.LocaleKey.Equals(localeKey,
                                                      StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

            var localeValueStr = localeValue.To<string>();

            if (prop != null)
            {
                if (string.IsNullOrWhiteSpace(localeValueStr))
                {
                    //delete
                    await DeleteLocalizedPropertyAsync(prop);
                }
                else
                {
                    //update
                    prop.LocaleValue = localeValueStr;
                    await UpdateLocalizedPropertyAsync(prop);
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(localeValueStr))
                {
                    //insert
                    prop = new TblLocalizedProperty()
                    {
                        EntityId = entity.Id,
                        LanguageId = languageId,
                        LocaleKey = localeKey,
                        LocaleKeyGroup = localeKeyGroup,
                        LocaleValue = localeValueStr
                    };
                    await InsertLocalizedPropertyAsync(prop);
                }
            }
        }

        public virtual async Task SaveLocalizedSettingAsync<T>(T setting, string localeKeyGroup, string localeKey, LocalizedString localizedString) where T : ISettings
        {
            foreach (var item in localizedString.Where(p => p.Key != 0))
            {
                var languageId = item.Key;

                var props = GetLocalizedProperties(0, localeKeyGroup);
                var prop = props.FirstOrDefault(lp => lp.LanguageId == languageId &&
                                                      lp.LocaleKey.Equals(localeKey,
                                                          StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

                var localeValueStr = item.Value;

                if (prop != null)
                {
                    if (string.IsNullOrWhiteSpace(localeValueStr))
                    {
                        //delete
                        await DeleteLocalizedPropertyAsync(prop);
                    }
                    else
                    {
                        //update
                        prop.LocaleValue = localeValueStr;
                        await UpdateLocalizedPropertyAsync(prop);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(localeValueStr))
                    {
                        //insert
                        prop = new TblLocalizedProperty()
                        {
                            EntityId = 0,
                            LanguageId = languageId,
                            LocaleKey = localeKey,
                            LocaleKeyGroup = localeKeyGroup,
                            LocaleValue = localeValueStr
                        };
                        await InsertLocalizedPropertyAsync(prop);
                    }
                }
            }
        }

        public virtual void SaveLocalizedSetting<T>(T setting, string localeKeyGroup, string localeKey, LocalizedString localizedString) where T : ISettings
        {
            foreach (var item in localizedString.Where(p => p.Key != 0))
            {
                var languageId = item.Key;

                var props = GetLocalizedProperties(0, localeKeyGroup);
                var prop = props.FirstOrDefault(lp => lp.LanguageId == languageId &&
                                                      lp.LocaleKey.Equals(localeKey,
                                                          StringComparison.InvariantCultureIgnoreCase)); //should be culture invariant

                var localeValueStr = item.Value;

                if (prop != null)
                {
                    if (string.IsNullOrWhiteSpace(localeValueStr))
                    {
                        //delete
                        DeleteLocalizedProperty(prop);
                    }
                    else
                    {
                        //update
                        prop.LocaleValue = localeValueStr;
                        UpdateLocalizedProperty(prop);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(localeValueStr))
                    {
                        //insert
                        prop = new TblLocalizedProperty()
                        {
                            EntityId = 0,
                            LanguageId = languageId,
                            LocaleKey = localeKey,
                            LocaleKeyGroup = localeKeyGroup,
                            LocaleValue = localeValueStr
                        };
                        InsertLocalizedProperty(prop);
                    }
                }
            }
        }

        public virtual IList<TblLocalizedProperty> GetLocalizedProperties(int entityId, string localeKeyGroup)
        {
            if (string.IsNullOrEmpty(localeKeyGroup))
                return new List<TblLocalizedProperty>();

            return _dbContext.LocalizedProperty.OrderBy(p => p.Id)
                .Where(p => p.EntityId == entityId &&
                            p.LocaleKeyGroup == localeKeyGroup)
                .FromCache(QueryCacheTag.LocalizedProperty).ToList();
        }

        public virtual async Task<IList<TblLocalizedProperty>> GetLocalizedPropertiesAsync(int entityId, string localeKeyGroup)
        {
            if (string.IsNullOrEmpty(localeKeyGroup))
                return new List<TblLocalizedProperty>();

            return (await _dbContext.LocalizedProperty.OrderBy(p => p.Id)
                .Where(p => p.EntityId == entityId &&
                            p.LocaleKeyGroup == localeKeyGroup)
                .FromCacheAsync(QueryCacheTag.LocalizedProperty)).ToList();
        }

        public virtual IList<TblLocalizedProperty> GetLocalizedProperties(int entityId, string localeKeyGroup, string localKey)
        {
            if (string.IsNullOrEmpty(localeKeyGroup))
                return new List<TblLocalizedProperty>();

            return _dbContext.LocalizedProperty.OrderBy(p => p.Id)
                .Where(p => p.EntityId == entityId &&
                            p.LocaleKeyGroup == localeKeyGroup &&
                            p.LocaleKey == localKey)
                .FromCache(QueryCacheTag.LocalizedProperty).ToList();
        }

        public virtual async Task<IList<TblLocalizedProperty>> GetLocalizedPropertiesAsync(int entityId, string localeKeyGroup, string localKey)
        {
            if (string.IsNullOrEmpty(localeKeyGroup))
                return new List<TblLocalizedProperty>();

            return (await _dbContext.LocalizedProperty.OrderBy(p => p.Id)
                .Where(p => p.EntityId == entityId &&
                            p.LocaleKeyGroup == localeKeyGroup &&
                            p.LocaleKey == localKey)
                .FromCacheAsync(QueryCacheTag.LocalizedProperty)).ToList();
        }
    }
}
