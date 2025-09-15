using Discord.WebSocket;

namespace RaceControlBot.HelperFunctions
{
    public static class RoleCheck
    {
        /// <summary>
        /// What a lad this is no, it just makes the comma list into a ulong array by splitting and trimming the string
        /// </summary>
        /// <param name="csv">Just put comma seperated list of role ids here and be happy</param>
        /// <returns></returns>
        public static ulong[] ParseRoleIds(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return Array.Empty<ulong>();

            string[] parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            List<ulong> ids = new List<ulong>(parts.Length);

            foreach (string part in parts)
            {
                ulong id;
                if (ulong.TryParse(part, out id))
                    ids.Add(id);
            }

            return ids.ToArray();
        }

        /// <summary>
        /// Returns true if the member has any of the provided role IDs.
        /// Set requireAll = true to require all roles instead.
        /// </summary>
        public static bool HasRoles(SocketGuildUser member, IEnumerable<ulong> roleIds, bool requireAll = false)
        {
            if (member == null)
            {
                return false;
            }
            if (roleIds == null)
            {
                return false;
            }

            List<ulong> rolesToCheck = new List<ulong>(roleIds);
            if (rolesToCheck.Count == 0)
            {
                return false;
            }

            HashSet<ulong> memberRoleIds = new HashSet<ulong>(member.Roles.Select(r => r.Id));
  
            //Console.WriteLine("rolesToCheck: " + string.Join(", ", rolesToCheck));
            //Console.WriteLine("memberRoleIds: " + string.Join(", ", memberRoleIds));
            if (requireAll)
            {
                foreach (ulong id in roleIds)
                    if (!memberRoleIds.Contains(id))
                    {
                        return false;
                    }
                return true;
            }
            else
            {
                foreach (ulong id in roleIds)
                    if (memberRoleIds.Contains(id))
                    {
                        return true;
                    }
                return false;
            }
        }
    }
}
