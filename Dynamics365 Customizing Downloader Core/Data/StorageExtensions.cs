﻿//-----------------------------------------------------------------------
// <copyright file="StorageExtensions.cs" company="https://github.com/jhueppauff/Dynamics365-Customizing-Downloader">
// Copyright 2018 Jhueppauff
// Mozilla Public License Version 2.0 
// For licence details visit https://github.com/jhueppauff/Dynamics365-Customizing-Downloader/blob/master/LICENSE
// </copyright>
//-----------------------------------------------------------------------

namespace Dynamics365CustomizingDownloader.Core.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// JSON.Net Storage Adapter for saving CRM Connection to file
    /// </summary>
    public static class StorageExtensions
    {
        /// <summary>
        /// Log4Net Logger
        /// </summary>
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Local Path of the JSON Storage File
        /// </summary>
        private static string storagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Dyn365_Configuration.json");

        /// <summary>
        /// Gets the local path of the JSON Storage File
        /// </summary>
        public static string StoragePath
        {
            get
            {
                return storagePath;
            }
        }

        /// <summary>
        /// Saves the <see cref="Xrm.CrmConnection"/> to the local Configuration
        /// </summary>
        /// <param name="crmConnection">CRM Connection <see cref="Xrm.CrmConnection"/></param>
        /// <param name="encryptionKey">The Encryption key used in <see cref="Core.Data.Cryptography"/></param>
        public static void Save(Xrm.CrmConnection crmConnection, string encryptionKey)
        {
            List<Xrm.CrmConnection> crmConnections = new List<Xrm.CrmConnection>();

            // Create if File does not exist
            if (!File.Exists(StoragePath))
            {
                File.Create(StoragePath).Close();

                // Encrypt Connection String
                crmConnection.ConnectionString = Cryptography.EncryptStringAES(crmConnection.ConnectionString, encryptionKey);
                crmConnection.LocalPath = string.Empty;
                crmConnections.Add(crmConnection);
                string json = JsonConvert.SerializeObject(crmConnections);

                File.WriteAllText(StoragePath, json);
            }
            else
            {
                using (StreamReader streamReader = new StreamReader(Data.StorageExtensions.StoragePath))
                {
                    string localJSON = streamReader.ReadToEnd();

                    // Convert Json to List
                    crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(localJSON);

                    // Encrypt Connection String
                    crmConnection.ConnectionString = Cryptography.EncryptStringAES(crmConnection.ConnectionString, encryptionKey);
                    crmConnection.LocalPath = string.Empty;
                    crmConnections.Add(crmConnection);

                    // Close File Stream
                    streamReader.Close();
                }

                // Write to Configuration File
                string json = JsonConvert.SerializeObject(crmConnections);
                File.WriteAllText(StoragePath, json);
            }
        }

        /// <summary>
        /// Loads the current configuration
        /// </summary>
        /// <param name="encryptionKey">The Encryption key used in <see cref="Core.Data.Cryptography"/></param>
        /// <returns>Returns <see cref="List{Xrm.CrmConnection}"/></returns>
        public static List<Xrm.CrmConnection> Load(string encryptionKey)
        {
            List<Xrm.CrmConnection> crmConnections;

            if (File.Exists(StoragePath))
            {
                using (StreamReader streamReader = new StreamReader(Data.StorageExtensions.StoragePath))
                {
                    string json = streamReader.ReadToEnd();

                    // Converts Json to List
                    crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(json);

                    foreach (Xrm.CrmConnection crmTempConnection in crmConnections)
                    {
                        crmTempConnection.ConnectionString = Cryptography.DecryptStringAES(crmTempConnection.ConnectionString, encryptionKey);
                    }

                    // Close File Stream
                    streamReader.Close();
                }
            }
            else
            {
                StorageExtensions.Log.Error("File was not found", new FileNotFoundException("File was not found", StoragePath));
                throw new FileNotFoundException("File was not found", StoragePath);
            }

            return crmConnections;
        }

        /// <summary>
        /// Updates an existing CRM Connection
        /// </summary>
        /// <param name="crmConnection">CRM Connection <see cref="Xrm.CrmConnection"/></param>
        /// <param name="encryptionKey">The Encryption key used in <see cref="Core.Data.Cryptography"/></param>
        public static void Update(Xrm.CrmConnection crmConnection, string encryptionKey)
        {
            string json = File.ReadAllText(StoragePath);
            List<Xrm.CrmConnection> crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(json);

            crmConnections.Find(x => x.ConnectionID == crmConnection.ConnectionID).ConnectionString = Cryptography.EncryptStringAES(crmConnection.ConnectionString, encryptionKey);
            crmConnections.Find(x => x.ConnectionID == crmConnection.ConnectionID).LocalPath = crmConnection.LocalPath;
            crmConnections.Find(x => x.ConnectionID == crmConnection.ConnectionID).Name  = crmConnection.Name;

            string jsonNew = JsonConvert.SerializeObject(crmConnections);
            File.WriteAllText(StoragePath, jsonNew);
        }

        /// <summary>
        /// Gets the Connection ID by the Connection Name
        /// </summary>
        /// <param name="connectionName"><see cref="string"/> Connection Name</param>
        /// <returns>Returns the <see cref="Guid"/> of the connection.</returns>
        public static Guid FindConnectionIDByName(string connectionName)
        {
            string json = File.ReadAllText(StoragePath);
            List<Xrm.CrmConnection> crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(json);

            // Add GUID if it does not exists
            if (crmConnections.Find(x => x.Name == connectionName).ConnectionID == Guid.Empty)
            {
                Guid connectionID = Guid.NewGuid();
                
                crmConnections.Find(x => x.Name == connectionName).ConnectionID = connectionID;

                string jsonNew = JsonConvert.SerializeObject(crmConnections);
                File.WriteAllText(StoragePath, jsonNew);
            }

            return crmConnections.Find(x => x.Name == connectionName).ConnectionID;
        }

        /// <summary>
        /// Adds a Guid to a Connection if no GUID exists
        /// </summary>
        /// <param name="connectionName">Name Connection</param>
        /// <returns>Returns the <see cref="Guid"/>of the Updated Connection</returns>
        public static Guid UpdateConnectionWithID(string connectionName)
        {
            Guid connectionID = Guid.NewGuid();
            string json = File.ReadAllText(StoragePath);
            List<Xrm.CrmConnection> crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(json);

            if (crmConnections.Find(x => x.Name == connectionName).ConnectionID == Guid.Empty)
            {
                crmConnections.Find(x => x.Name == connectionName).ConnectionID = connectionID;

                string jsonNew = JsonConvert.SerializeObject(crmConnections);
                File.WriteAllText(StoragePath, jsonNew);

                return connectionID;
            }
            else
            {
                return Guid.Empty;
            }
        }

        /// <summary>
        /// Gets the count of all Connections with the specified connectionName
        /// </summary>
        /// <param name="connectionName">Name of the Connection</param>
        /// <returns>Returns a count <see cref="int"/> of all found connections</returns>
        public static int GetCountOfConnectionNamesByName(string connectionName)
        {
            string json = File.ReadAllText(StoragePath);
            List<Xrm.CrmConnection> crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(json);

            int count = 0;

            count = crmConnections.Count(x => x.Name == connectionName);

            return count;
        }

        /// <summary>
        /// Deletes an Item from the Configuration
        /// </summary>
        /// <param name="connectionName">Name of the Connection</param>
        /// <param name="wipeData">if this is set, all data within the local folder will be wiped out!</param>
        public static void Delete(string connectionName, bool wipeData = false)
        {
            if (File.Exists(StoragePath))
            {
                string json = File.ReadAllText(StoragePath);
                List<Xrm.CrmConnection> crmConnections = JsonConvert.DeserializeObject<List<Xrm.CrmConnection>>(json);

                try
                {
                    Xrm.CrmConnection crmConnection = crmConnections.SingleOrDefault(x => x.Name == connectionName);
                    crmConnections.Remove(crmConnection);

                    if (wipeData)
                    {
                        Directory.Delete(crmConnection.LocalPath, true);
                    }

                    if (crmConnections.Count == 0)
                    {
                        // Delete File if there is no Connection left
                        File.Delete(StoragePath);
                    }
                    else
                    {
                        string jsonNew = JsonConvert.SerializeObject(crmConnections);
                        File.WriteAllText(StoragePath, jsonNew);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.Message, ex);
                    throw;
                }
            }
        }
    }
}