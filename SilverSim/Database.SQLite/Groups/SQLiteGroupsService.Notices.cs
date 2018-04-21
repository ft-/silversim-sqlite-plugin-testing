// SilverSim is distributed under the terms of the
// GNU Affero General Public License v3 with
// the following clarification and special exception.

// Linking this library statically or dynamically with other modules is
// making a combined work based on this library. Thus, the terms and
// conditions of the GNU Affero General Public License cover the whole
// combination.

// As a special exception, the copyright holders of this library give you
// permission to link this library with independent modules to produce an
// executable, regardless of the license terms of these independent
// modules, and to copy and distribute the resulting executable under
// terms of your choice, provided that you also meet, for each linked
// independent module, the terms and conditions of the license of that
// module. An independent module is a module which is not derived from
// or based on this library. If you modify this library, you may extend
// this exception to your version of the library, but you are not
// obligated to do so. If you do not wish to do so, delete this
// exception statement from your version.

using SilverSim.ServiceInterfaces.Groups;
using SilverSim.Types;
using SilverSim.Types.Groups;
using System.Collections.Generic;
using System.Data.SQLite;

namespace SilverSim.Database.SQLite.Groups
{
    public sealed partial class SQLiteGroupsService : GroupsServiceInterface.IGroupNoticesInterface
    {
        GroupNotice IGroupNoticesInterface.this[UGUI requestingAgent, UUID groupNoticeID]
        {
            get
            {
                var notice = new GroupNotice();
                if (!Notices.TryGetValue(requestingAgent, groupNoticeID, out notice))
                {
                    throw new KeyNotFoundException();
                }
                return notice;
            }
        }

        void IGroupNoticesInterface.Add(UGUI requestingAgent, GroupNotice notice)
        {
            var vals = new Dictionary<string, object>
            {
                ["GroupID"] = notice.Group.ID,
                ["NoticeID"] = notice.ID,
                ["Timestamp"] = notice.Timestamp,
                ["FromName"] = notice.Timestamp,
                ["Subject"] = notice.Subject,
                ["Message"] = notice.Message,
                ["HasAttachment"] = notice.HasAttachment,
                ["AttachmentType"] = notice.AttachmentType,
                ["AttachmentName"] = notice.AttachmentName,
                ["AttachmentItemID"] = notice.AttachmentItemID,
                ["AttachmentOwnerID"] = notice.AttachmentOwner.ID
            };
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                conn.InsertInto("groupnotices", vals);
            }
        }

        void IGroupNoticesInterface.Delete(UGUI requestingAgent, UUID groupNoticeID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("DELETE FROM groupinvites WHERE InviteID = @inviteid", conn))
                {
                    cmd.Parameters.AddParameter("@inviteid", groupNoticeID);
                    if (cmd.ExecuteNonQuery() < 1)
                    {
                        throw new KeyNotFoundException();
                    }
                }
            }
        }

        List<GroupNotice> IGroupNoticesInterface.GetNotices(UGUI requestingAgent, UGI group)
        {
            var notices = new List<GroupNotice>();
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM groupnotices WHERE GroupID = @groupid", conn))
                {
                    cmd.Parameters.AddParameter("@groupid", group.ID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            GroupNotice notice = reader.ToGroupNotice();
                            notice.Group = ResolveName(requestingAgent, notice.Group);
                            notices.Add(notice);
                        }
                    }
                }
            }

            return notices;
        }

        bool IGroupNoticesInterface.TryGetValue(UGUI requestingAgent, UUID groupNoticeID, out GroupNotice groupNotice)
        {
            GroupNotice notice;
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT * FROM groupnotices WHERE NoticeID = @noticeid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@noticeid", groupNoticeID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            groupNotice = null;
                            return false;
                        }
                        notice = reader.ToGroupNotice();
                        notice.Group = ResolveName(requestingAgent, notice.Group);
                    }
                }
            }

            groupNotice = notice;
            return true;
        }

        bool IGroupNoticesInterface.ContainsKey(UGUI requestingAgent, UUID groupNoticeID)
        {
            using (var conn = new SQLiteConnection(m_ConnectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand("SELECT NoticeID FROM groupnotices WHERE NoticeID = @noticeid LIMIT 1", conn))
                {
                    cmd.Parameters.AddParameter("@noticeid", groupNoticeID);
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        return reader.Read();
                    }
                }
            }
        }
    }
}
