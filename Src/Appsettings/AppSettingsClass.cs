using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Web;
using System.Web.Caching;
using System.Collections;

namespace Appsettings
{
    internal class AppSettingsClass<TEntity> : AppSettingsBase
    {
        /// <summary>
        /// TEntity type的缓存类
        /// </summary>
        static Dictionary<string, string> TTypeCache = new Dictionary<string, string>();

        /// <summary>
        /// 缓存Key
        /// </summary>
        private string Key
        {
            get { return "APPSETTINGSLIST_Class_" + this.GetClassName(); }
        }

        /// <summary>
        /// 获取自定义配置实体信息
        /// </summary>
        /// <param name="xmlSubPath">配置文件中的相对位置</param>
        /// <returns></returns>
        public TEntity GetEntity(string xmlSubPath = null)
        {
            var appSettings = HttpRuntime.Cache.Get(Key);
            if (appSettings == null)
            {
                appSettings = LoadToCache(xmlSubPath);
            }

            var entitys = appSettings as List<TEntity>;
            if (entitys != null)
            {
                return entitys.FirstOrDefault();
            }
            return default(TEntity);
        }

        /// <summary>
        /// 获取自定义配置实体信息
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="xmlSubPath">配置文件中的相对位置</param>
        /// <returns></returns>
        public TEntity GetEntity(Func<TEntity, bool> predicate, string xmlSubPath = null)
        {
            var appSettings = HttpRuntime.Cache.Get(Key);
            if (appSettings == null)
            {
                appSettings = LoadToCache(xmlSubPath);
            }

            var entitys = appSettings as List<TEntity>;
            if (entitys != null)
            {
                return entitys.FirstOrDefault(predicate);
            }
            return default(TEntity);
        }

        /// <summary>
        /// 获取自定义配置实体信息集合
        /// </summary>
        /// <param name="xmlSubPath">配置文件中的相对位置</param>
        /// <returns></returns>
        public List<TEntity> GetEntityList(string xmlSubPath = null)
        {
            var appSettings = HttpRuntime.Cache.Get(Key);
            if (appSettings == null)
            {
                appSettings = LoadToCache(xmlSubPath);
            }
            return appSettings as List<TEntity>;
        }

        /// <summary>
        /// 获取自定义配置实体信息集合
        /// </summary>
        /// <param name="predicate"></param>
        /// <param name="xmlSubPath"></param>
        /// <returns></returns>
        public List<TEntity> GetEntityList(Func<TEntity, bool> predicate, string xmlSubPath = null)
        {
            var appSettings = HttpRuntime.Cache.Get(Key);
            if (appSettings == null)
            {
                appSettings = LoadToCache(xmlSubPath);
            }

            var entitys = appSettings as List<TEntity>;
            if (entitys != null)
            {
                return entitys.Where(predicate).ToList();
            }
            return null;
        }

        /// <summary>
        /// 加载自定义配置到缓存
        /// </summary>
        /// <param name="xmlSubPath">配置文件中的相对位置</param>
        /// <returns></returns>
        public List<TEntity> LoadToCache(string xmlSubPath = null)
        {
            try
            {
                var appSettings = GetAppSettings(xmlSubPath);
                if (HttpRuntime.Cache[Key] != null)
                {
                    HttpRuntime.Cache.Remove(Key);
                }

                if (appSettings != null && appSettings.Count > 0)
                {
                    CacheDependency cdd = new CacheDependency(XmlPaths); //缓存依赖文件
                    HttpRuntime.Cache.Insert(Key, appSettings, cdd, DateTime.MaxValue, Cache.NoSlidingExpiration);
                }
                return appSettings;
            }
            catch (Exception ex)
            {
                AppSettingsBase.Log(ex);
                throw ex;
            }
        }

        /// <summary>
        /// 加载自定义配置列表
        /// </summary>
        /// <param name="xmlSubPath">配置文件中的相对位置</param>
        /// <returns></returns>
        private List<TEntity> GetAppSettings(string xmlSubPath = null)
        {
            List<TEntity> result = new List<TEntity>();

            foreach (var xmlPath in XmlPaths)
            {
                if (!xmlPath.EndsWithIgnoreCase(".xml")) continue;

                XDocument doc = AppSettingsUtils.LoadXml(xmlPath);
                var appSettings = doc.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("AppSettings"));
                if (appSettings == null) continue;

                List<XElement> elements = appSettings.ToList();
                if (string.IsNullOrEmpty(xmlSubPath))
                {
                    elements = elements.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase(this.GetClassName())).ToList();
                }
                else
                {
                    //var test = appSettings
                    //    .Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("DataSyncConfig"))
                    //    .Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("StationMapping"))
                    //    .Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("StationItem"));

                    var TEntityName = GetTType<TEntity>();
                    if (!xmlSubPath.EndsWithIgnoreCase(TEntityName))
                    {
                        xmlSubPath = xmlSubPath + "." + TEntityName;
                    }
                    var arr = xmlSubPath.Split('.');
                    foreach (var sub in arr)
                    {
                        if (sub.EqualsIgnoreCase("AppSettings")) { continue; }
                        elements = elements.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase(sub)).ToList();
                    }
                }

                if (elements.Count == 0) continue;

                PropertyInfo[] Propertes = this.GetPropertys();
                foreach (XElement element in elements)
                {
                    var obj = (TEntity)BuildObj(typeof(TEntity), Propertes, element);
                    result.Add(obj);
                }
            }
            return result;
        }

        /// <summary>
        /// 集合
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private IList BuildList(List<XElement> elements, Type type)
        {
            Type listType = typeof(List<>);
            listType = listType.MakeGenericType(type);
            IList list = Activator.CreateInstance(listType) as IList;

            if (elements.Count == 0) return list;

            //该类型的属性
            PropertyInfo[] Propertes = type.GetProperties();
            //逐行解析
            foreach (XElement element in elements)
            {
                var obj = BuildObj(type, Propertes, element);
                list.Add(obj);
            }
            return list;
        }

        /// <summary>
        /// 单个对象
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private object BuildObj(List<XElement> elements, Type type)
        {
            if (elements.Count == 0) return null;
            var element = elements[0];

            //该类型的属性
            PropertyInfo[] Propertes = type.GetProperties();
            var obj = BuildObj(type, Propertes, element);
            return obj;
        }

        /// <summary>
        /// 单个对象
        /// </summary>
        /// <param name="type"></param>
        /// <param name="Propertes"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        private object BuildObj(Type type, PropertyInfo[] Propertes, XElement element)
        {
            //得到XML中的所有属性
            var attributes = element.Attributes().ToList();
            //创建实例
            var obj = type.Assembly.CreateInstance(type.FullName);
            foreach (var current in Propertes)
            {
                PropertyInfo propertyInfo = current;
                var attribute = attributes.FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase(propertyInfo.Name));
                //如果是泛型（暂时仅仅实现复杂类型）
                if (propertyInfo.PropertyType.IsGenericType && !IsBasicType(propertyInfo.PropertyType.GetGenericArguments()[0]))
                {
                    //得到泛型的T的类型
                    var type2 = propertyInfo.PropertyType.GetGenericArguments()[0];
                    //得到T的对于的XML元素集合
                    var elements2 = element.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase(type2.Name)).ToList();
                    //构建集合
                    var subList = BuildList(elements2, type2);
                    //给属性设置值
                    propertyInfo.SetValue(obj, subList, null);
                }
                //如果是自定义类型
                else if (!propertyInfo.PropertyType.IsGenericType && !IsBasicType(propertyInfo.PropertyType))
                {
                    //得到T的对应的XML元素集合
                    var elements2 = element.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase(propertyInfo.PropertyType.Name)).ToList();
                    //构建对象
                    var innerObj = BuildObj(elements2, propertyInfo.PropertyType);
                    //给属性设置值
                    propertyInfo.SetValue(obj, innerObj, null);
                }
                else
                {
                    //简单类型的属性
                    if (attribute != null)
                    {
                        propertyInfo.SetValue(obj, GetDefaultValue(attribute.Value, propertyInfo.PropertyType), null);
                    }
                }
            }

            return obj;
        }

        #region 反射帮助

        /// <summary>
        /// 获取类名
        /// </summary>
        /// <returns></returns>
        private string GetClassName()
        {
            return typeof(TEntity).Name;
        }

        /// <summary>
        /// 获取类属性
        /// </summary>
        /// <returns></returns>
        private PropertyInfo[] GetPropertys()
        {
            return typeof(TEntity).GetProperties();
        }

        /// <summary>
        /// 获取默认值
        /// </summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private object GetDefaultValue(object value, Type type)
        {
            try
            {
                //obj =  Convert.ChangeType(obj, type);
                value = AppSettingsBase.ChangeValueType(value, type);
            }
            catch (Exception)
            {
                value = default(object);
            }
            return value;
        }

        #endregion

        private bool IsBasicType(Type type)
        {
            switch (type.Name)
            {
                case "Boolean":
                case "Byte":
                case "Char":
                case "DateTime":
                case "Decimal":
                case "Double":
                case "Int16":
                case "Int32":
                case "Int64":
                case "SByte":
                case "Single":
                case "String":
                case "UInt16":
                case "UInt32":
                case "UInt64":
                    {
                        return true;
                    }
                default:
                    return false;
            }
        }

        static object lockObj = new object();
        /// <summary>
        /// 获取对象的名称
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        static string GetTType<T>()
        {
            lock (lockObj)
            {
                var type = typeof(T);
                if (TTypeCache.ContainsKey(type.FullName))
                {
                    return TTypeCache[type.FullName];
                }
                else
                {
                    TTypeCache[type.FullName] = type.Name;
                    return type.Name;
                }
            }
        }
    }
}
