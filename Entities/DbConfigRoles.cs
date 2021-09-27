using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace RRBot.Entities
{
    [FirestoreData]
    public class DbConfigRoles
    {
        [FirestoreDocumentId]
        public DocumentReference Reference { get; set; }
        [FirestoreProperty("djRole")]
        public ulong DJRole { get; set; }
        [FirestoreProperty("houseRole")]
        public ulong StaffLvl1Role { get; set; }
        [FirestoreProperty("mutedRole")]
        public ulong MutedRole { get; set; }
        [FirestoreProperty("senateRole")]
        public ulong StaffLvl2Role { get; set; }

        public static async Task<DbConfigRoles> GetById(ulong guildId)
        {
            DocumentReference doc = Program.database.Collection($"servers/{guildId}/config").Document("channels");
            DocumentSnapshot snap = await doc.GetSnapshotAsync();
            if (snap.Exists)
                return snap.ConvertTo<DbConfigRoles>();
            await doc.CreateAsync(new { djRole = 0UL });
            return await GetById(guildId);
        }

        public async Task Write() => await Reference.SetAsync(this);
    }
}