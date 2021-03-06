﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Microsoft.VisualStudio.Text;
using Vim;

namespace VimCoreTest
{
    [TestFixture]
    public class SnapshotLineUtilTest
    {
        static string[] s_lines = new string[]
            {
                "summary description for this line",
                "some other line",
                "running out of things to make up"
            };

        ITextBuffer _buffer = null;
        ITextSnapshot _snapshot = null;

        public void Create(params string[] lines)
        {
            _buffer = Utils.EditorUtil.CreateBuffer(lines);
            _snapshot = _buffer.CurrentSnapshot;
        }

        [TearDown]
        public void TearDown()
        {
            _buffer = null;
            _snapshot = null;
        }

        [Test]
        public void GetPoints1()
        {
            Create("foo");
            var points = SnapshotLineUtil.GetPoints(_buffer.CurrentSnapshot.GetLineFromLineNumber(0));
            var text = points.Select(x => x.GetChar().ToString()).Aggregate((x, y) => x + y);
            Assert.AreEqual("foo",text);
        }

        [Test]
        public void GetExtent1()
        {
            Create("foo");
            var span = SnapshotLineUtil.GetExtent(_buffer.GetLine(0));
            Assert.AreEqual("foo", span.GetText());
        }

        [Test]
        public void GetExtentIncludingLineBreak1()
        {
            Create("foo", "baz");
            var span = SnapshotLineUtil.GetExtentIncludingLineBreak(_buffer.GetLine(0));
            Assert.AreEqual("foo" + Environment.NewLine, span.GetText());
        }


    }
}
