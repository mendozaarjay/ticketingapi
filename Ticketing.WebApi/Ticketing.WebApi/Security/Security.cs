using System;
using System.Collections;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Ticketing.WebApi
{
    public partial class Security
    {
        public static string Encrypt(string content)
        {
            string passPhrase = "´Å×™ÔÖ¤×É";
            string saltValue = "×¤•ØºÅÐÙÉ";
            string hashAlgorithm = "SHA1";
            int passwordIterations = 2;
            string initVector = "¤•¦–Ç—¨˜É™ªšË›¬œ";
            int keySize = 128;
            if (content == null)
                return (string)null;
            if (content == "")
                return "";
            return Security.Encrypt(content, passPhrase, saltValue, hashAlgorithm, passwordIterations, initVector, keySize);
        }

        public static string Decrypt(string encryptedContent)
        {
            string passPhrase = "´Å×™ÔÖ¤×É";
            string saltValue = "×¤•ØºÅÐÙÉ";
            string hashAlgorithm = "SHA1";
            int passwordIterations = 2;
            string initVector = "¤•¦–Ç—¨˜É™ªšË›¬œ";
            int keySize = 128;
            if (encryptedContent == null)
                return (string)null;
            if (encryptedContent == "")
                return "";
            return Security.Decrypt(encryptedContent, passPhrase, saltValue, hashAlgorithm, passwordIterations, initVector, keySize);
        }
        public static string EncryptToBase64(string value)
        {
            var item = Encrypt(value);
            var baseitem = Encoding.UTF8.GetBytes(item);
            var result = Convert.ToBase64String(baseitem);
            return result;
        }
        public static string DecryptFromBase64(string value)
        {
            var item = Convert.FromBase64String(value);
            var newitem = Encoding.UTF8.GetString(item);
            var result = Decrypt(newitem);
            return result;
        }

        private static string RSAEncryptString(string inputString, string publicKeyFilePath)
        {
            StreamReader streamReader = new StreamReader(publicKeyFilePath, true);
            string end = streamReader.ReadToEnd();
            streamReader.Close();
            string oldValue = end.Substring(0, end.IndexOf("</BitStrength>") + 14);
            int int32 = Convert.ToInt32(oldValue.Replace("<BitStrength>", "").Replace("</BitStrength>", ""));
            string xmlString = end.Replace(oldValue, "");
            RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(int32);
            cryptoServiceProvider.FromXmlString(xmlString);
            int num1 = int32 / 8;
            byte[] bytes = Encoding.UTF32.GetBytes(inputString);
            int num2 = num1 - 42;
            int length = bytes.Length;
            int num3 = length / num2;
            StringBuilder stringBuilder = new StringBuilder();
            for (int index = 0; index <= num3; ++index)
            {
                byte[] rgb = new byte[length - num2 * index > num2 ? num2 : length - num2 * index];
                Buffer.BlockCopy((Array)bytes, num2 * index, (Array)rgb, 0, rgb.Length);
                byte[] inArray = cryptoServiceProvider.Encrypt(rgb, true);
                Array.Reverse((Array)inArray);
                stringBuilder.Append(Convert.ToBase64String(inArray));
            }
            return stringBuilder.ToString();
        }

        private static string RSADecryptString(string inputString, string privateKeyFilePath)
        {
            StreamReader streamReader = new StreamReader(privateKeyFilePath, true);
            string end = streamReader.ReadToEnd();
            streamReader.Close();
            string oldValue = end.Substring(0, end.IndexOf("</BitStrength>") + 14);
            int int32 = Convert.ToInt32(oldValue.Replace("<BitStrength>", "").Replace("</BitStrength>", ""));
            RSACryptoServiceProvider cryptoServiceProvider = new RSACryptoServiceProvider(int32);
            string xmlString = end.Replace(oldValue, "");
            cryptoServiceProvider.FromXmlString(xmlString);
            int length = int32 / 8 % 3 != 0 ? int32 / 8 / 3 * 4 + 4 : int32 / 8 / 3 * 4;
            int num = inputString.Length / length;
            ArrayList arrayList = new ArrayList();
            for (int index = 0; index < num; ++index)
            {
                byte[] rgb = Convert.FromBase64String(inputString.Substring(length * index, length));
                Array.Reverse((Array)rgb);
                arrayList.AddRange((ICollection)cryptoServiceProvider.Decrypt(rgb, true));
            }
            return Encoding.UTF32.GetString(arrayList.ToArray(Type.GetType("System.Byte")) as byte[]);
        }

        private static string Encrypt(string plainText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
        {
            byte[] bytes1 = Encoding.ASCII.GetBytes(initVector);
            byte[] bytes2 = Encoding.ASCII.GetBytes(saltValue);
            byte[] bytes3 = Encoding.UTF8.GetBytes(plainText);
            byte[] bytes4 = new PasswordDeriveBytes(passPhrase, bytes2, hashAlgorithm, passwordIterations).GetBytes(keySize / 8);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.Mode = CipherMode.CBC;
            ICryptoTransform encryptor = rijndaelManaged.CreateEncryptor(bytes4, bytes1);
            MemoryStream memoryStream = new MemoryStream();
            CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, encryptor, CryptoStreamMode.Write);
            cryptoStream.Write(bytes3, 0, bytes3.Length);
            cryptoStream.FlushFinalBlock();
            byte[] array = memoryStream.ToArray();
            memoryStream.Close();
            cryptoStream.Close();
            return Convert.ToBase64String(array);
        }

        private static string Decrypt(string cipherText, string passPhrase, string saltValue, string hashAlgorithm, int passwordIterations, string initVector, int keySize)
        {
            byte[] bytes1 = Encoding.ASCII.GetBytes(initVector);
            byte[] bytes2 = Encoding.ASCII.GetBytes(saltValue);
            byte[] buffer = Convert.FromBase64String(cipherText);
            byte[] bytes3 = new PasswordDeriveBytes(passPhrase, bytes2, hashAlgorithm, passwordIterations).GetBytes(keySize / 8);
            RijndaelManaged rijndaelManaged = new RijndaelManaged();
            rijndaelManaged.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = rijndaelManaged.CreateDecryptor(bytes3, bytes1);
            MemoryStream memoryStream = new MemoryStream(buffer);
            CryptoStream cryptoStream = new CryptoStream((Stream)memoryStream, decryptor, CryptoStreamMode.Read);
            byte[] numArray = new byte[buffer.Length];
            int count = cryptoStream.Read(numArray, 0, numArray.Length);
            memoryStream.Close();
            cryptoStream.Close();
            rijndaelManaged.Clear();
            return Encoding.UTF8.GetString(numArray, 0, count);
        }

    }
}
