using Lively.Common.Helpers.Storage;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Lively.Common.Helpers
{
    //Ref: https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.protecteddata
    public static class EncryptUtil
    {
        // Create byte array for additional entropy when using Protect method.
        private static readonly byte[] additionalEntropy = Encoding.UTF8.GetBytes(Constants.SingleInstance.UniqueAppName);

        public static byte[] Protect(byte[] data) => ProtectedData.Protect(data, additionalEntropy, DataProtectionScope.CurrentUser);

        public static byte[] Unprotect(byte[] data) => ProtectedData.Unprotect(data, additionalEntropy, DataProtectionScope.CurrentUser);

        public static byte[] Protect<T>(T data) => Protect(SerializeObject(data));

        public static T Unprotect<T>(byte[] data) => DeserializeObject<T>(Unprotect(data));

        public static void Store<T>(T data, string filePath) => JsonStorage<byte[]>.StoreData(filePath, Protect<T>(data));

        public static T Load<T>(string filePath) => Unprotect<T>(JsonStorage<byte[]>.LoadData(filePath));

        #region helpers

        private static byte[] SerializeObject<T>(T value) => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));

        private static T DeserializeObject<T>(byte[] value) => JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(value));

        #endregion //helpers
    }
}
