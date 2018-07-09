using Microsoft.AspNetCore.Http;
using MongoDB.Driver;
using System;

namespace EzSvrEngine.Extension {

    public static class GeneralExtension {

        public const int COLLECTION_SARDING_SLICE_AMOUNT = 100;

        public static String GetIP(this HttpContext content) {
            String ip = content.Request.Headers["X-Real-IP"];

            if (string.IsNullOrEmpty(ip)) {
                ip = content.Connection.RemoteIpAddress.ToString();
            }

            return ip;
        }


        /// <summary>
        /// 取得字符串长度，一个汉字按2
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int GetLayoutLen(this string str) {
            if (string.IsNullOrEmpty(str))
                return 0;

            var len = 0;
            var bytes = System.Text.Encoding.ASCII.GetBytes(str);
            foreach (var b in bytes) {
                if (b == 63)
                    len += 2;
                else
                    len += 1;
            }

            return len;
        }

        
        public static IMongoCollection<TDocument> GetCollectionSlice<TDocument>(this IMongoDatabase mongoDatabase, string name, int uid = 0) {
            
            var key = name;
            // 根据user的id进行分表 
            // history --> history_mod_1
            var slice = (uid % COLLECTION_SARDING_SLICE_AMOUNT);
            if (slice == 0) slice = COLLECTION_SARDING_SLICE_AMOUNT;
            key = $"{name}_mod_{slice}";
            return mongoDatabase.GetCollection<TDocument>(key);
        }
    }
}
