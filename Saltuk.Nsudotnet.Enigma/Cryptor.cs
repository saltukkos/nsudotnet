﻿using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace Saltuk.Nsudotnet.Enigma
{
    public class Cryptor
    {
        private class CryptSettings
        {
            public SymmetricAlgorithm Algorithm { get; set; }
            public string InputFilename { get; set; }
            public string OutputFilename { get; set; }
            public string KeyFileName { get; set; }
            public bool IsEncrypting { get; set; }
        }


        private static void Main(string[] args)
        {
            CryptSettings settings;
            if (!TryParseArguments(args, out settings))
            {
                Console.WriteLine("Incorrect parameters");
                return;
            }

            using (var inFile = new FileStream(settings.InputFilename, FileMode.Open))
            using (var outFile = new FileStream(settings.OutputFilename, FileMode.Create))
            using (var key = settings.IsEncrypting ?
                new FileStream(settings.OutputFilename + ".key", FileMode.Create) : 
                new FileStream(settings.KeyFileName, FileMode.Open) )
            {

                if (settings.IsEncrypting)
                    Encrypt(settings.Algorithm, inFile, outFile, key, d => Console.WriteLine("Progress: {0}", d)).RunSynchronously();
                else
                    Decrypt(settings.Algorithm, inFile, outFile, key, d => Console.WriteLine("Progress: {0}", d)).RunSynchronously();
                    
            }
        }

        public static async Task Encrypt(SymmetricAlgorithm algorithm, Stream input, Stream output, Stream key, Action<double> progressCallback)
        {
            using (var crypted = new CryptoStream(output, algorithm.CreateEncryptor(), CryptoStreamMode.Write))
            {
                long ready = 0;
                long size = input.Length;
                var buff = new byte[1024 * 100];
                while (ready < size)
                {
                    var n = await input.ReadAsync(buff, 0, buff.Length);
                    await crypted.WriteAsync(buff, 0, n);
                    ready += n;
                    progressCallback.Invoke((double)ready / size);
                }
            }

            using (var keyWriter = new StreamWriter(key))
            {
                keyWriter.WriteLine(Convert.ToBase64String(algorithm.Key));
                keyWriter.WriteLine(Convert.ToBase64String(algorithm.IV));
            }
        }

        public static async Task Decrypt(SymmetricAlgorithm algorithm, Stream input, Stream output, Stream key, Action<double> progressCallback)
        {
            using (var keyReader = new StreamReader(key))
            {
                var keyByte = Convert.FromBase64String(keyReader.ReadLine());
                var ivByte = Convert.FromBase64String(keyReader.ReadLine());
                algorithm.IV = ivByte;
                algorithm.Key = keyByte;

                using (var encrypted = new CryptoStream(output, algorithm.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    long ready = 0;
                    long size = input.Length;
                    var buff = new byte[1024 * 100];
                    while (ready < size)
                    {
                        var n = await input.ReadAsync(buff, 0, buff.Length);
                        await encrypted.WriteAsync(buff, 0, n);
                        ready += n;
                        progressCallback.Invoke((double)ready / size);
                    }
                }

            }
        }

        public static SymmetricAlgorithm ByName(string name)
        {
            if (name.Equals("AES", StringComparison.OrdinalIgnoreCase))
                return Aes.Create();
            if (name.Equals("DES", StringComparison.OrdinalIgnoreCase))
                return DES.Create();
            if (name.Equals("RC2", StringComparison.OrdinalIgnoreCase))
                return RC2.Create();
            if (name.Equals("Rijndael", StringComparison.OrdinalIgnoreCase))
                return Rijndael.Create();
            return null;
        }

        private static bool TryParseArguments(string[] args, out CryptSettings settings)
        {
            settings = new CryptSettings();

            if (args.Length < 4)
                return false;
            settings.InputFilename = args[1];
            settings.OutputFilename = args[3];

            if (args[0].Equals("encrypt", StringComparison.OrdinalIgnoreCase))
                settings.IsEncrypting = true;
            else if (args[0].Equals("decrypt", StringComparison.OrdinalIgnoreCase))
                settings.IsEncrypting = false;
            else
                return false;

            if (!settings.IsEncrypting)
            {
                if (args.Length < 5)
                    return false;
                settings.KeyFileName = args[4];
            }

            settings.Algorithm = ByName(args[2]);
            return settings.Algorithm != null;
        }

    }
}
