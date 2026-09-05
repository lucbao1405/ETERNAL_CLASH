using System;
using System.Collections.Generic;


namespace EternalClash.Core.Services
{
    /// <summary>
    /// Service container trung tâm.
    /// Thay thế dần Singleton.Instance.
    /// </summary>
    public static class ServiceRegistry
    {

        private static readonly Dictionary<Type, object> services =
            new Dictionary<Type, object>();



        /// <summary>
        /// Đăng ký một service.
        /// </summary>
        public static void Register<T>(T service)
        {
            Type type = typeof(T);


            if (services.ContainsKey(type))
            {
                UnityEngine.Debug.LogWarning(
                    $"Service {type.Name} đã tồn tại."
                );

                return;
            }


            services.Add(type, service);
        }



        /// <summary>
        /// Lấy service.
        /// </summary>
        public static T Get<T>()
        {
            Type type = typeof(T);


            if (services.TryGetValue(type, out object value))
            {
                return (T)value;
            }


            throw new Exception(
                $"Không tìm thấy service: {type.Name}"
            );
        }



        /// <summary>
        /// Kiểm tra service tồn tại.
        /// </summary>
        public static bool Has<T>()
        {
            return services.ContainsKey(typeof(T));
        }



        /// <summary>
        /// Xóa toàn bộ service.
        /// Dùng khi reset game.
        /// </summary>
        public static void Clear()
        {
            services.Clear();
        }
    }
}