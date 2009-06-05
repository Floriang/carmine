
namespace Saxx.Carmine.Plugins {
    internal class Tweet {

        public Tweet(string from, string status, int id) {
            From = from;
            Status = status;
            Id = id;
        }

        public string From {
            get;
            private set;
        }

        public string Status {
            get;
            private set;
        }

        public int Id {
            get;
            private set;
        }
    }
}
