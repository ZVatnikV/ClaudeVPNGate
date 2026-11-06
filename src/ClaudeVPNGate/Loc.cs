namespace ClaudeVPNGate;

public static class Loc
{
    public static string Lang { get; set; } = "en";
    public static event Action? Changed;
    private static readonly Dictionary<string, Dictionary<string, string>> Packs = new()
    {
        ["en"] = new()
        {
            ["dash"] = "Dashboard", ["settings"] = "Settings", ["adapters"] = "Adapters",
            ["hint"] = "Claude stays sealed until a tunnel owns the default route.",
            ["hintset"] = "The check looks at the default route, not at a VPN window.",
            ["hintad"] = "Only the winning default route can open Claude.",
            ["quit"] = "Quit and seal", ["adminT"] = "Not protecting yet",
            ["adminB"] = "Windows, Linux, or macOS needs administrator rights before the seal is real. Do not open Claude yet.",
            ["adminBtn"] = "Restart as administrator",
            ["vpn"] = "VPN route", ["vpnD"] = "The tunnel must own the default route.",
            ["claude"] = "Claude", ["claudeD"] = "Sealed means no request is allowed out.",
            ["guard"] = "Guard", ["guardD"] = "This window keeps watching.",
            ["adapter"] = "Adapter", ["adapterD"] = "Interface that currently wins the route.",
            ["events"] = "Events", ["interval"] = "Poll interval (seconds)",
            ["keys"] = "Extra adapter keywords", ["ignore"] = "Ignore adapters",
            ["login"] = "Start with the system", ["save"] = "Save", ["saved"] = "Saved",
            ["note"] = "The close button minimizes the window. Quit seals Claude until you start the guard again and the tunnel is up.",
            ["sealed"] = "SEALED", ["open"] = "OPEN", ["wait"] = "WAITING",
            ["subS"] = "No request can leave", ["subO"] = "Tunnel owns the route", ["subW"] = "Administrator required",
            ["on"] = "tunnel", ["off"] = "none", ["gS"] = "sealed", ["gO"] = "allowed", ["run"] = "running", ["arm"] = "not armed",
            ["scan"] = "Refresh", ["up"] = "Up", ["down"] = "Down", ["route"] = "Route",
        },
        ["ru"] = new()
        {
            ["dash"] = "Панель", ["settings"] = "Параметры", ["adapters"] = "Адаптеры",
            ["hint"] = "Claude закрыт, пока туннель не держит маршрут по умолчанию.",
            ["hintset"] = "Проверка смотрит на маршрут, а не на окно VPN.",
            ["hintad"] = "Открыть Claude может только победивший маршрут.",
            ["quit"] = "Выйти и закрыть", ["adminT"] = "Защиты ещё нет",
            ["adminB"] = "Нужны права администратора, иначе печать не ставится. До этого Claude не открывать.",
            ["adminBtn"] = "Запустить от администратора",
            ["vpn"] = "Маршрут VPN", ["vpnD"] = "Туннель должен держать маршрут по умолчанию.",
            ["claude"] = "Claude", ["claudeD"] = "Закрыт — запрос наружу запрещён.",
            ["guard"] = "Страж", ["guardD"] = "Это окно продолжает следить.",
            ["adapter"] = "Адаптер", ["adapterD"] = "Интерфейс, который сейчас выиграл маршрут.",
            ["events"] = "События", ["interval"] = "Интервал опроса (сек)",
            ["keys"] = "Доп. слова адаптеров", ["ignore"] = "Игнорировать адаптеры",
            ["login"] = "Запуск вместе с системой", ["save"] = "Сохранить", ["saved"] = "Сохранено",
            ["note"] = "Крестик сворачивает окно. Выход снова закрывает Claude, пока программа не запущена и туннель не поднят.",
            ["sealed"] = "ЗАКРЫТ", ["open"] = "ОТКРЫТ", ["wait"] = "ЖДЁМ",
            ["subS"] = "Запрос не уйдёт", ["subO"] = "Туннель держит маршрут", ["subW"] = "Нужен администратор",
            ["on"] = "туннель", ["off"] = "нет", ["gS"] = "закрыт", ["gO"] = "можно", ["run"] = "работает", ["arm"] = "не вооружён",
            ["scan"] = "Обновить", ["up"] = "Вкл", ["down"] = "Выкл", ["route"] = "Маршрут",
        },
    };

    public static string T(string key) => Packs[Lang].TryGetValue(key, out var v) ? v : key;
    public static void Set(string lang) { Lang = lang == "ru" ? "ru" : "en"; Changed?.Invoke(); }
}
