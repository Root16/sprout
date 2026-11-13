using Spectre.Console;
using Spectre.Console.Rendering;
using Root16.Sprout.Extensions;

namespace Root16.Sprout.SpectreConsole.ProgressColumns;

public class StepDescriptionColumn : ProgressColumn
{

    public Justify Alignment { get; set; } = Justify.Right;

    public override IRenderable Render(RenderOptions options, ProgressTask task, TimeSpan deltaTime)
    {
        var text = task.Description?.RemoveNewLines()?.Trim();
        return new Markup(text ?? string.Empty).Overflow(Overflow.Ellipsis).Justify(Alignment);
    }
}
