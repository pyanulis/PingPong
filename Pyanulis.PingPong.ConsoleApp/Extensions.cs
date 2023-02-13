using EasyNetQ.Management.Client.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Pyanulis.PingPong.ConsoleApp
{
    internal static class Extensions
    {
        public static string GetName(this Binding b)
        {
            return $"{b.Source} => ({b.RoutingKey}) => {b.Destination} [{b.DestinationType}]";
        }

        public static string TrimName(this string command)
        {
            return command.Substring(command.IndexOf(' ')).Trim();
        }

        public static string[] Params(this string command)
        {
            return command.TrimName().Split(' ');
        }

        public static bool IsCommand(this string command, string name)
        {
            return command.StartsWith(name);
        }
    }
}
