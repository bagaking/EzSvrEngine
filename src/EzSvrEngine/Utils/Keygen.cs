namespace EzSvrEngine.Utils {

    public enum KeyGenIDType {
        user,
        club
    }

    public class KeyGen {

        private enum PeriodSection {
            once,
            daily,
            weekly,
            monthly,
            period,
        }

        private KeyGen() { }

        private string primary { get; set; }
        private string title { get; set; }
        private string period_section { get; set; }
        private string aggregator { get; set; }

        private static KeyGen New => new KeyGen();
        public static KeyGen Once => New.SetPeriod(PeriodSection.once);
        public static KeyGen Daily => New.SetPeriod(PeriodSection.daily);
        public static KeyGen Weekly => New.SetPeriod(PeriodSection.weekly);
        public static KeyGen Monthly => New.SetPeriod(PeriodSection.monthly);
        public static KeyGen Period => New.SetPeriod(PeriodSection.period);

        private KeyGen SetPeriod(PeriodSection _period_section) {
            period_section = $"{_period_section}";
            return this;
        }


        public KeyGen Primary(string _primary) {
            primary = $"{_primary}";
            return this;
        }


        public KeyGen Title(string _title) {
            title = $"{_title}";
            return this;
        }

        public KeyGen Aggregator(KeyGenIDType type, int id) {
            aggregator = $"{type.ToString()}-{id}";
            return this;
        }

        private static string Build(string str) {
            return string.IsNullOrWhiteSpace(str) ? "" : $"{str}-";
        }

        public override string ToString() {
            return $"{Build(primary)}{Build(period_section)}{Build(title)}{Build(aggregator ?? "all")}";
        }


        public static implicit operator string(KeyGen k) => k.ToString();
    }
}
