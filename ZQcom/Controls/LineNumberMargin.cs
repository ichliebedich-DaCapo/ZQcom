using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows;

namespace ZQcom.Controls
{

    // 行号边距类
    //public class LineNumberMargin : AbstractMargin
    //{
    //    protected override void OnRender(DrawingContext drawingContext)
    //    {
    //        base.OnRender(drawingContext);

    //        // 获取当前的文本编辑器
    //        var textEditor = (TextEditor)AssociatedObject;
    //        if (textEditor == null || textEditor.Document == null)
    //            return;

    //        // 获取当前视图中的行号
    //        var visibleLines = textEditor.TextView.VisibleLines;
    //        var lineCount = visibleLines.Count;

    //        // 计算边距的宽度
    //        double marginWidth = 0;
    //        for (int i = 0; i < lineCount; i++)
    //        {
    //            int lineNumber = visibleLines[i].FirstDocumentLine.LineNumber + 1;
    //            string lineNumberText = lineNumber.ToString();
    //            marginWidth = Math.Max(marginWidth, textEditor.TextView.GetGlyphBounds(lineNumberText).Width);
    //        }

    //        // 设置边距的宽度
    //        this.Width = marginWidth + 10; // 10 是额外的间距

    //        // 绘制行号
    //        for (int i = 0; i < lineCount; i++)
    //        {
    //            int lineNumber = visibleLines[i].FirstDocumentLine.LineNumber + 1;
    //            string lineNumberText = lineNumber.ToString();

    //            // 计算绘制位置
    //            var rect = new Rect(0, visibleLines[i].VisualTop, marginWidth, visibleLines[i].Height);
    //            drawingContext.DrawText(textEditor.TextView.DefaultForegroundBrush, textEditor.TextView.FormattedTextFactory.CreateFormattedText(lineNumberText, textEditor.TextView.Foreground), rect.TopLeft);
    //        }
    //    }

    //    public override double Width
    //    {
    //        get { return base.Width; }
    //        set
    //        {
    //            base.Width = value;
    //            InvalidateArrange();
    //        }
    //    }
    //}
}
