﻿using System;
using System.Collections.Generic;
using EVESharp.Common.Database;
using EVESharp.Node.StaticData.Certificates;
using MySql.Data.MySqlClient;
using EVESharp.Node.StaticData;
using EVESharp.PythonTypes.Types.Database;

namespace EVESharp.Node.Database
{
    public class CertificatesDB : DatabaseAccessor
    {
        /// <summary>
        /// Get list of certificates granted to the given character, ready for the EVE Client
        /// </summary>
        /// <param name="characterID"></param>
        /// <returns></returns>
        public Rowset GetMyCertificates(int characterID)
        {
            return Database.PrepareRowsetQuery(
                "SELECT certificateID, grantDate, visibilityFlags FROM chrCertificates WHERE characterID = @characterID",
                new Dictionary<string, object>()
                {
                    {"@characterID", characterID}
                }
            );
        }

        /// <summary>
        /// Loads the full list of relationships for the certificates
        /// </summary>
        /// <returns></returns>
        public Dictionary<int, List<Relationship>> GetCertificateRelationships()
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.Select(ref connection, "SELECT relationshipID, parentID, parentTypeID, parentLevel, childID, childTypeID FROM crtRelationships");
            
            using (connection)
            using (reader)
            {
                Dictionary<int, List<Relationship>> result = new Dictionary<int, List<Relationship>>();
                
                while (reader.Read() == true)
                {
                    if (result.TryGetValue(reader.GetInt32(4), out var relationships) == false)
                        relationships = result[reader.GetInt32(4)] = new List<Relationship>();
                    
                    relationships.Add(new Relationship()
                        {
                            RelationshipID = reader.GetInt32(0),
                            ParentID = reader.GetInt32(1),
                            ParentTypeID = reader.GetInt32(2),
                            ParentLevel = reader.GetInt32(3),
                            ChildID = reader.GetInt32(4),
                            ChildTypeID = reader.GetInt32(5)
                        }
                    );
                }

                return result;
            }
        }

        /// <summary>
        /// Creates a new record granting a certificate to the player
        /// </summary>
        /// <param name="characterID">The player to grant the certificate to</param>
        /// <param name="certificateID">The certificate to grant the player</param>
        public void GrantCertificate(int characterID, int certificateID)
        {
            Database.PrepareQuery(
                "REPLACE INTO chrCertificates(characterID, certificateID, grantDate, visibilityFlags)VALUES(@characterID, @certificateID, @grantDate, 0)",
                new Dictionary<string, object>()
                {
                    {"@characterID", characterID},
                    {"@certificateID", certificateID},
                    {"@grantDate", DateTime.UtcNow.ToFileTimeUtc ()}
                }
            );
        }

        /// <summary>
        /// Updates the visibility flags of the given certificate
        /// </summary>
        /// <param name="certificateID">The certificate to update</param>
        /// <param name="characterID">The character to update the cert for</param>
        /// <param name="visibilityFlags">The new visibility settings</param>
        public void UpdateVisibilityFlags(int certificateID, int characterID, int visibilityFlags)
        {
            Database.PrepareQuery(
                "UPDATE chrCertificates SET visibilityFlags=@visibilityFlags WHERE characterID=@characterID AND certificateID=@certificateID",
                new Dictionary<string, object>()
                {
                    {"@visibilityFlags", visibilityFlags},
                    {"@characterID", characterID},
                    {"@certificateID", certificateID}
                }
            );
        }

        /// <summary>
        /// Obtains a list of all the certificates a character has ready to be sent to the EVE Client
        /// </summary>
        /// <param name="characterID"></param>
        /// <returns></returns>
        public Rowset GetCertificatesByCharacter(int characterID)
        {
            return Database.PrepareRowsetQuery(
                "SELECT certificateID, grantDate, visibilityFlags FROM chrCertificates WHERE characterID = @characterID AND visibilityFlags = 1",
                new Dictionary<string, object>()
                {
                    {"@characterID", characterID}
                }
            );
        }

        /// <summary>
        /// Obtains the list of certificates a charactert has
        /// </summary>
        /// <param name="characterID"></param>
        /// <returns></returns>
        public List<int> GetCertificateListForCharacter(int characterID)
        {
            MySqlConnection connection = null;
            MySqlDataReader reader = Database.Select(ref connection,
                "SELECT certificateID FROM chrCertificates WHERE characterID = @characterID",
                new Dictionary<string, object>()
                {
                    {"@characterID", characterID}
                }
            );
            
            using(connection)
            using (reader)
            {
                List<int> certificates = new List<int>();
                
                while (reader.Read() == true)
                    certificates.Add(reader.GetInt32(0));

                return certificates;
            }
        }

        public CertificatesDB(DatabaseConnection db) : base(db)
        {
        }
    }
}