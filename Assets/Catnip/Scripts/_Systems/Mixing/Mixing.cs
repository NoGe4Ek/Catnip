using System;
using System.Collections.Generic;

namespace Catnip.Scripts._Systems.Mixing {
public class Mixing {
    [Serializable]
    public class Mix {
        public MixBase mixBase;
        public List<MixComponent> mixComponents = new List<MixComponent>();
    }

    public enum MixBase {
        Empty,
        Catnip, // Кошкачья мята
        Valerian, // Валериана
        Hops // Хмель
    }

    public enum MixComponent {
        Chamomile, // Ромашка
        Lavender, // Лаванда
        Peppermint, // Мята перечная
        LemonBalm, // Мелисса (Лимонная мята)
        Sage, // Шалфей
        Thyme // Тимьян (Чабрец)
    }

    public enum Addiction {
        Low,
        Medium,
        High
    }

    public enum Effect {
        JumpingAbility,
        SpeedAbility
    }
}
}