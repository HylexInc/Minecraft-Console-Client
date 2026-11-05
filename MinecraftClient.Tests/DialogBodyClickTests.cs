using MinecraftClient.Dialogs;
using MinecraftClient.Protocol.Dialogs;
using MinecraftClient.Scripting;

namespace MinecraftClient.Tests;

public sealed class DialogBodyClickTests
{
    private static DialogDefinition ParseNotice(params object[] body)
    {
        return new DialogNbtParser().Parse(new Dictionary<string, object>
        {
            ["type"] = "minecraft:notice",
            ["title"] = "Menu",
            ["body"] = body
        });
    }

    private static Dictionary<string, object> PlainMessage(object contents)
    {
        return new Dictionary<string, object>
        {
            ["type"] = "minecraft:plain_message",
            ["contents"] = contents
        };
    }

    [Fact]
    public void ModernCustomClickKeepsIdPayloadTextAndHover()
    {
        var payload = new Dictionary<string, object> { ["mode"] = "duels" };
        var dialog = ParseNotice(PlainMessage(new Dictionary<string, object>
        {
            ["text"] = "Pick: ",
            ["extra"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["text"] = "",
                    ["extra"] = new object[] { "Duels" },
                    ["click_event"] = new Dictionary<string, object>
                    {
                        ["action"] = "custom",
                        ["id"] = "example:menu/duels",
                        ["payload"] = payload
                    },
                    ["hover_event"] = new Dictionary<string, object>
                    {
                        ["action"] = "show_text",
                        ["value"] = new Dictionary<string, object> { ["text"] = "§aDuels\n§7Click to play" }
                    }
                }
            }
        }));

        var click = Assert.Single(dialog.BodyClicks);
        Assert.Equal(1, click.Index);
        Assert.Equal(1, click.BodyIndex);
        Assert.Equal("Duels", click.Text);
        Assert.Equal("Duels\nClick to play", click.Hover);
        Assert.Equal(DialogActionKind.CustomClick, click.Kind);
        Assert.Equal("custom", click.Action);
        Assert.Equal("example:menu/duels", click.Id);
        Assert.Same(payload, click.Payload);
        Assert.Equal("Pick: Duels", ChatBot.GetVerbatim(dialog.Body[0].Text));
    }

    [Fact]
    public void LegacyEventsAreAccepted()
    {
        var dialog = ParseNotice(PlainMessage(new Dictionary<string, object>
        {
            ["text"] = "Spawn",
            ["clickEvent"] = new Dictionary<string, object> { ["action"] = "run_command", ["value"] = "/spawn" },
            ["hoverEvent"] = new Dictionary<string, object> { ["action"] = "show_text", ["contents"] = "Go home" }
        }));

        var click = Assert.Single(dialog.BodyClicks);
        Assert.Equal(DialogActionKind.RunCommand, click.Kind);
        Assert.Equal("/spawn", click.Value);
        Assert.Equal("Spawn", click.Text);
        Assert.Equal("Go home", click.Hover);
    }

    [Fact]
    public void NestedClicksAreNumberedInOrderAcrossBodyElements()
    {
        var dialog = ParseNotice(
            PlainMessage(new Dictionary<string, object>
            {
                ["text"] = "Shop ",
                ["click_event"] = new Dictionary<string, object> { ["action"] = "open_url", ["url"] = "https://example.com" },
                ["hover_event"] = new Dictionary<string, object> { ["action"] = "show_text", ["value"] = "Open the shop" },
                ["extra"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["text"] = "VIP",
                        ["click_event"] = new Dictionary<string, object> { ["action"] = "suggest_command", ["command"] = "/vip" }
                    }
                }
            }),
            PlainMessage(new Dictionary<string, object>
            {
                ["text"] = "Copy",
                ["click_event"] = new Dictionary<string, object> { ["action"] = "copy_to_clipboard", ["value"] = "abc" }
            }));

        var clicks = dialog.BodyClicks;
        Assert.Equal(new[] { 1, 2, 3 }, clicks.Select(static click => click.Index));
        Assert.Equal(new[] { 1, 1, 2 }, clicks.Select(static click => click.BodyIndex));
        Assert.Equal("Shop ", clicks[0].Text);
        Assert.Equal("https://example.com", clicks[0].Value);
        Assert.Equal("VIP", clicks[1].Text);
        Assert.Equal("Open the shop", clicks[1].Hover);
        Assert.Equal(DialogActionKind.SuggestCommand, clicks[1].Kind);
        Assert.Equal(DialogActionKind.CopyToClipboard, clicks[2].Kind);
        Assert.Equal("abc", clicks[2].Value);
    }

    [Fact]
    public void ItemDescriptionAndInlineShowDialogAreParsed()
    {
        var dialog = ParseNotice(new Dictionary<string, object>
        {
            ["type"] = "minecraft:item",
            ["description"] = new Dictionary<string, object>
            {
                ["contents"] = new Dictionary<string, object>
                {
                    ["text"] = "Rules",
                    ["click_event"] = new Dictionary<string, object>
                    {
                        ["action"] = "show_dialog",
                        ["dialog"] = new Dictionary<string, object> { ["type"] = "minecraft:notice", ["title"] = "Server rules" }
                    }
                }
            }
        });

        var click = Assert.Single(dialog.BodyClicks);
        Assert.Equal(DialogActionKind.ShowDialog, click.Kind);
        Assert.Equal("Rules", click.Text);
        Assert.Equal("Server rules", click.NestedDialog?.Title);
    }
}
