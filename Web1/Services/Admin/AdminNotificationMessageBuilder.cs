namespace Web1.Services.Admin;

public static class AdminNotificationMessageBuilder
{
    public static (string Title, string Message) Build(
        string category,
        string action,
        string planName,
        string? itemLabel,
        bool? checklistDone)
    {
        var item = string.IsNullOrWhiteSpace(itemLabel) ? null : $"\"{itemLabel.Trim()}\"";
        return category switch
        {
            "admin_plan_basic" => PlanBasic(action, planName),
            "admin_plan_notes" => PlanNotes(planName),
            "admin_destination" => Destination(action, planName, item),
            "admin_expense" => Expense(action, planName, item),
            "admin_activity" => Activity(action, planName, item),
            "admin_checklist" => Checklist(action, planName, item, checklistDone),
            _ => (
                "Administrator je izmenio plan",
                $"Administrator je izvrsio izmene na vasem putovanju \"{planName}\".")
        };
    }

    private static (string, string) PlanBasic(string action, string planName) =>
        action switch
        {
            "Created" => (
                "Novo putovanje",
                $"Administrator je kreirao putovanje \"{planName}\" na vasem nalogu."),
            "Deleted" => (
                "Administrator je obrisao putovanje",
                $"Administrator je obrisao vase putovanje \"{planName}\"."),
            _ => (
                "Izmena osnovnih podataka",
                $"Administrator je izmenio osnovne podatke (naziv, datume ili budzet) vaseg putovanja \"{planName}\".")
        };

    private static (string, string) PlanNotes(string planName) =>
        (
            "Izmena napomena",
            $"Administrator je izvrsio izmene u napomenama vaseg putovanja \"{planName}\".");

    private static (string, string) Destination(string action, string planName, string? item) =>
        action switch
        {
            "Created" => (
                "Nova destinacija",
                $"Administrator je dodao destinaciju {item ?? ""} u sekciji Destinacije za vase putovanje \"{planName}\"."),
            "Updated" => (
                "Izmena destinacije",
                $"Administrator je izmenio destinaciju {item ?? ""} u sekciji Destinacije vaseg putovanja \"{planName}\"."),
            _ => (
                "Brisanje destinacije",
                $"Administrator je obrisao destinaciju {item ?? ""} iz sekcije Destinacije vaseg putovanja \"{planName}\".")
        };

    private static (string, string) Expense(string action, string planName, string? item) =>
        action switch
        {
            "Created" => (
                "Novi trosak",
                $"Administrator je dodao trosak {item ?? ""} u sekciji Troskovi i budzet vaseg putovanja \"{planName}\"."),
            "Updated" => (
                "Izmena troska",
                $"Administrator je izmenio trosak {item ?? ""} u sekciji Troskovi i budzet vaseg putovanja \"{planName}\"."),
            _ => (
                "Brisanje troska",
                $"Administrator je obrisao trosak {item ?? ""} iz sekcije Troskovi i budzet vaseg putovanja \"{planName}\".")
        };

    private static (string, string) Activity(string action, string planName, string? item) =>
        action switch
        {
            "Created" => (
                "Nova aktivnost",
                $"Administrator je dodao aktivnost {item ?? ""} u sekciji Aktivnosti po danima (kalendar) za putovanje \"{planName}\"."),
            "Updated" => (
                "Izmena aktivnosti",
                $"Administrator je izmenio aktivnost {item ?? ""} u sekciji Aktivnosti po danima za putovanje \"{planName}\"."),
            _ => (
                "Brisanje aktivnosti",
                $"Administrator je obrisao aktivnost {item ?? ""} iz sekcije Aktivnosti po danima vaseg putovanja \"{planName}\".")
        };

    private static (string, string) Checklist(string action, string planName, string? item, bool? done) =>
        action switch
        {
            "Created" => (
                "Nova stavka u checklisti",
                $"Administrator je dodao stavku {item ?? ""} u vasu checklistu (packing) za putovanje \"{planName}\"."),
            "Updated" => (
                "Izmena checkliste",
                $"Administrator je izmenio stavku {item ?? ""} u vasoj checklisti (packing) za putovanje \"{planName}\"."),
            "Toggled" => done == true
                ? (
                    "Checklista azurirana",
                    $"Administrator je oznacio stavku {item ?? ""} kao zavrsenu u vasoj checklisti za putovanje \"{planName}\".")
                : (
                    "Checklista azurirana",
                    $"Administrator je ponistio oznaku zavrseno za stavku {item ?? ""} u vasoj checklisti za putovanje \"{planName}\"."),
            _ => (
                "Brisanje iz checkliste",
                $"Administrator je obrisao stavku {item ?? ""} iz vase checkliste (packing) za putovanje \"{planName}\".")
        };
}
