﻿using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Json;

namespace KtaneWeb
{
    public sealed partial class KtanePropellerModule
    {
        private HttpResponse mainPage(HttpRequest req, KtaneWebConfigEntry config)
        {
            // Access keys:
            // A
            // B
            // C
            // D    sort by defuser difficulty
            // E    sort by expert difficulty
            // F
            // G
            // H
            // I    Include missing
            // J    JSON
            // K
            // L
            // M    Manual
            // N    sort by name
            // O    Mods
            // P    Twitch Plays only
            // Q
            // R    Regular
            // S
            // T    Tutorial video
            // U    Source code
            // V    Vanilla
            // W    Steam Workshop Item
            // X
            // Y    Needy
            // Z
            // .    More

            var sheets = config.KtaneModules.ToDictionary(mod => mod.Name, mod => config.EnumerateSheetUrls(mod.Name, config.KtaneModules.Select(m => m.Name).Where(m => m != mod.Name && m.StartsWith(mod.Name)).ToArray()));

            var selectables = Ut.NewArray(
                new Selectable
                {
                    HumanReadable = "Manual",
                    Accel = 'M',
                    Icon = mod => new IMG { class_ = "icon manual-icon", title = "Manual", alt = "Manual", src = sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["icon"].GetString() : null },
                    DataAttributeName = "manual",
                    DataAttributeValue = mod => sheets.Get(mod.Name, null)?.ToString(),
                    Url = mod => sheets[mod.Name].Count > 0 ? sheets[mod.Name][0]["url"].GetString() : null,
                    ShowIcon = mod => sheets[mod.Name].Count > 0,
                    CssClass = "manual"
                },
                new Selectable
                {
                    HumanReadable = "Steam Workshop",
                    Accel = 'W',
                    Icon = mod => new IMG { class_ = "icon", title = "Steam Workshop", alt = "Steam Workshop", src = "HTML/img/steam-workshop-item.png" },
                    DataAttributeName = "steam",
                    DataAttributeValue = mod => mod.SteamID?.Apply(s => $"http://steamcommunity.com/sharedfiles/filedetails/?id={s}"),
                    Url = mod => $"http://steamcommunity.com/sharedfiles/filedetails/?id={mod.SteamID}",
                    ShowIcon = mod => mod.SteamID != null
                },
                new Selectable
                {
                    HumanReadable = "Source code",
                    Accel = 'u',
                    Icon = mod => new IMG { class_ = "icon", title = "Source code", alt = "Source code", src = "HTML/img/unity.png" },
                    DataAttributeName = "source",
                    DataAttributeValue = mod => mod.SourceUrl,
                    Url = mod => mod.SourceUrl,
                    ShowIcon = mod => mod.SourceUrl != null
                },
                new Selectable
                {
                    HumanReadable = "Tutorial video",
                    Accel = 'T',
                    Icon = mod => new IMG { class_ = "icon", title = "Tutorial video", alt = "Tutorial video", src = "HTML/img/video.png" },
                    DataAttributeName = "video",
                    DataAttributeValue = mod => mod.TutorialVideoUrl,
                    Url = mod => mod.TutorialVideoUrl,
                    ShowIcon = mod => mod.TutorialVideoUrl != null
                });

            var filters = Ut.NewArray(
                KtaneFilter.Checkboxes("type", "Type", mod => mod.Type),
                KtaneFilter.Checkboxes("origin", "Origin", mod => mod.Origin),
                KtaneFilter.Slider("defdiff", "Defuser difficulty", mod => mod.DefuserDifficulty),
                KtaneFilter.Slider("expdiff", "Expert difficulty", mod => mod.ExpertDifficulty),
                KtaneFilter.Boolean("twitchplays", "Twitch Plays only", mod => mod.HasTwitchPlaysSupport, 'P'));

            return HttpResponse.Html(new HTML(
                new HEAD(
                    new TITLE("Repository of Manual Pages"),
                    new LINK { href = req.Url.WithParent("HTML/css/font.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new LINK { href = req.Url.WithParent("css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPT { src = "HTML/js/jquery.3.1.1.min.js" },
                    new SCRIPT { src = "HTML/js/jquery-ui.1.12.1.min.js" },
                    new LINK { href = req.Url.WithParent("HTML/css/jquery-ui.1.12.1.css").ToHref(), rel = "stylesheet", type = "text/css" },
                    new SCRIPTLiteral($@"
                        Ktane = {{
                            Filters: {filters.Select(f => f.ToJson()).ToJsonList()},
                            Selectables: {selectables.Select(s => s.DataAttributeName).ToJsonList()}
                        }};
                    "),
                    new SCRIPT { src = req.Url.WithParent("js").ToHref() },
                    new META { name = "viewport", content = "width=device-width; initial-scale=1.0" }),
                new BODY(
                    new DIV { id = "main-content" }._(
                        filters
                            .Select(filter => new DIV { class_ = "filter-section" }._(filter.ToHtml()))
                            .ToArray()
                            .Apply(filterUis =>
                                new TABLE { class_ = "header" }._(
                                    new TR(
                                        new TD { class_ = "logo" }._(new IMG { class_ = "logo", src = "HTML/img/repo-logo.png" }),
                                        new TD { class_ = "selectables move-mobile" }._(
                                            new H4("Make links go to:"),
                                            selectables.Select(sel => new DIV(
                                                new INPUT { type = itype.radio, class_ = "set-selectable", name = "selectable", id = $"selectable-{sel.DataAttributeName}" }.Data("selectable", sel.DataAttributeName), " ",
                                                new LABEL { class_ = "set-selectable", id = $"selectable-label-{sel.DataAttributeName}", for_ = $"selectable-{sel.DataAttributeName}", accesskey = sel.Accel.ToString().ToLowerInvariant() }._(sel.HumanReadable.Accel(sel.Accel)))),
                                            new DIV { id = "include-missing" }._(
                                                new INPUT { type = itype.checkbox, class_ = "filter", id = "filter-include-missing" }, " ",
                                                new LABEL { for_ = "filter-include-missing", accesskey = "i" }._("Include missing".Accel('I')))),
                                        new TD { class_ = "filters move-mobile" }._(filterUis[0], filterUis[1]),
                                        new TD { class_ = "filters move-mobile" }._(filterUis[2], filterUis[3], filterUis[4]),
                                        new TD { class_ = "sort move-mobile" }._(
                                            new H4("Sort order:"),
                                            new DIV(
                                                new INPUT { id = "sort-name", name = "sort", value = "name", class_ = "sort", type = itype.radio },
                                                new LABEL { for_ = "sort-name", accesskey = "n" }._("\u00a0Sort by name".Accel('n'))),
                                            new DIV(
                                                new INPUT { id = "sort-defuser-difficulty", name = "sort", value = "defuser-difficulty", class_ = "sort", type = itype.radio },
                                                new LABEL { for_ = "sort-defuser-difficulty", accesskey = "d" }._("\u00a0Sort by defuser difficulty".Accel('d'))),
                                            new DIV(
                                                new INPUT { id = "sort-expert-difficulty", name = "sort", value = "expert-difficulty", class_ = "sort", type = itype.radio },
                                                new LABEL { for_ = "sort-expert-difficulty", accesskey = "e" }._("\u00a0Sort by expert difficulty".Accel('e')))),
                                        new TD { class_ = "mobile-ui" }._(new A { href = "#", class_ = "mobile-opt", id = "page-opt" })))),
                        new DIV { id = "main-table-container" }._(
                            new DIV { id = "more-tab" }._(new A { href = "#", id = "more-link", accesskey = "." }._("More")),
                            new TABLE { id = "main-table" }._(
                                new TR { class_ = "header-row" }._(
                                    new TH { colspan = selectables.Length }._("Links"),
                                    new TH { class_ = "modlink" }._(new A { href = "#", class_ = "sort", id = "sort-by-name" }._("Name")),
                                    new TH { class_ = "infos" }._(new A { href = "#", class_ = "sort", id = "sort-by-difficulty" }._("Information"))),
                                config.KtaneModules.Select(mod =>
                                    new TR { class_ = "mod" }
                                        .Data("mod", mod.Name)
                                        .Data("sortkey", mod.SortKey)
                                        .AddData(selectables, sel => sel.DataAttributeName, sel => sel.DataAttributeValue(mod))
                                        .AddData(filters, flt => flt.DataAttributeName, flt => flt.GetDataAttributeValue(mod))
                                        ._(
                                            selectables.Select((sel, ix) => new TD { class_ = "selectable" + (ix == selectables.Length - 1 ? " last" : null) + sel.CssClass?.Apply(c => " " + c) }._(sel.ShowIcon(mod) ? new A { href = sel.Url(mod), class_ = sel.CssClass }._(sel.Icon(mod)) : null)),
                                            new TD { class_ = "modlink" }._(new DIV { class_ = "modlink-wrap" }._(new A { class_ = "modlink" }._(mod.Icon(config), new SPAN { class_ = "mod-name" }._(mod.Name)))),
                                            new TD { class_ = "infos" }._(
                                                new DIV { class_ = "inf-modlink" }._(new A { class_ = "modlink" }._(mod.Icon(config), new SPAN { class_ = "mod-name" }._(mod.Name))),
                                                new DIV { class_ = "inf-author" }._(mod.Author),
                                                new DIV { class_ = "inf-type" }._(mod.Type.ToString()),
                                                mod.DefuserDifficulty == mod.ExpertDifficulty
                                                    ? new DIV { class_ = "inf-difficulty" }._(new SPAN { class_ = "inf-difficulty-sub" }._(mod.DefuserDifficulty.ToReadable()))
                                                    : new DIV { class_ = "inf-difficulty" }._(new SPAN { class_ = "inf-difficulty-sub" }._(mod.DefuserDifficulty.ToReadable()), " (d), ", new SPAN { class_ = "inf-difficulty-sub" }._(mod.ExpertDifficulty.ToReadable()), " (e)")),
                                            new TD { class_ = "mobile-ui" }._(new A { href = "#", class_ = "mobile-opt" })))),
                            new DIV { id = "more", class_ = "popup disappear stay" }._(
                                new DIV { class_ = "close" },
                                new DIV { class_ = "icons" }._(
                                    new DIV { class_ = "icon" }._(new A { href = "https://steamcommunity.com/app/341800/workshop/" }._(new IMG { class_ = "icon", src = "HTML/img/steam-workshop.png" }, new SPAN("Steam Workshop"))),
                                    new DIV { class_ = "icon" }._(new A { href = "https://www.youtube.com/playlist?list=PL23fILnY52_2-I6JNG_7jw69x5YXj11GN" }._(new IMG { class_ = "icon", src = "HTML/img/video-playlist.png" }, new SPAN("Tutorial Videos Playlist"))),
                                    new DIV { class_ = "icon" }._(new A { href = "https://docs.google.com/document/d/1zObWfLI8RMiNL1b6AXfiy4cwjGD9H3oStPiZaEOS5Lc" }._(new IMG { class_ = "icon", src = "HTML/img/google-docs.png" }, new SPAN("Entering the World of Mods"))),
                                    new DIV { class_ = "icon" }._(new A { href = "More/Logfile%20Analyzer.html" }._(new IMG { class_ = "icon", src = "HTML/img/logfile-analyzer.png" }, new SPAN("Logfile Analyzer"))),
                                    new DIV { class_ = "icon" }._(new A { href = "https://discord.gg/Fv7YEDj" }._(new IMG { class_ = "icon", src = "HTML/img/discord.png" }, new SPAN("Join us on Discord")))),
                                new DIV { class_ = "dev" }._(
                                new DIV { class_ = "mobile-opts" },
                                    new SPAN { class_ = "dev-link" }._(new A { href = "https://form.jotform.com/62686042776162" }._("Submit an idea for a new mod")),
                                    new SPAN { class_ = "dev-link" }._(new A { href = "https://form.jotform.com/62718595122156" }._("Find a mod idea to implement"))),
                                new DIV { class_ = "highlighting-controls" }._(
                                    new H3("Controls to highlight elements in HTML manuals"),
                                    new TABLE { class_ = "highlighting-controls" }._(
                                        new TR(new TH("Control"), new TH("Function")),
                                        new TR(new TD("Ctrl+Click (Windows)", new BR(), "Command+Click (Mac)"), new TD("Highlight a table column")),
                                        new TR(new TD("Shift+Click"), new TD("Highlight a table row")),
                                        new TR(new TD("Alt+Click (Windows)", new BR(), "Ctrl+Shift+Click (Windows)", new BR(), "Command+Shift+Click (Mac)"), new TD("Highlight a table cell or an item in a list")))),
                                new H3("Default file locations"),
                                new H4("Output log"),
                                new TABLE { class_ = "file-locations" }._(
                                    new TR(new TH("Windows,\u00a0Steam:"), new TD(new CODE(@"C:\Program Files (x86)\Steam\steamapps\common\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt"))),
                                    new TR(new TH("Windows,\u00a0Oculus:"), new TD(new CODE(@"C:\Program Files (x86)\Oculus\Software\steel-crate-games-keep-talking-and-nobody-explodes\Keep Talking and Nobody Explodes\ktane_Data\output_log.txt"))),
                                    new TR(new TH("Mac:"), new TD(new CODE(@"~/Library/Logs/Unity/Player.log")))),
                                new H4("Screenshots"),
                                new TABLE { class_ = "file-locations" }._(
                                    new TR(new TH("Windows,\u00a0Steam:"), new TD(new CODE(@"C:\Program Files (x86)\Steam\userdata\<some number>\760\remote\341800\screenshots"))),
                                    new TR(new TH("Mac,\u00a0Steam:"), new TD(new CODE(@"~/Library/Application Support/Steam/userdata/<some number>/760/remote/341800/screenshots")))),
                                new DIV { class_ = "json" }._(new A { href = "/json", accesskey = "j" }._("See JSON".Accel('J'))),
                                new DIV { class_ = "icon-credits" }._("Module icons by lumbud84 and samfun123.")))))));
        }
    }
}
