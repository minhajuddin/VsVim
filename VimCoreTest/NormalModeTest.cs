﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Vim;
using Microsoft.VisualStudio.Text;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Text.Editor;
using VimCoreTest.Utils;
using Microsoft.FSharp.Core;
using Moq;
using MockFactory = VimCoreTest.Utils.MockObjectFactory;
using Vim.Modes.Normal;
using Vim.Modes;
using Microsoft.VisualStudio.Text.Operations;
using Vim.Extensions;

namespace VimCoreTest
{
    [TestFixture]
    public class NormalModeTest
    {
        private Vim.Modes.Normal.NormalMode _modeRaw;
        private INormalMode _mode;
        private IWpfTextView _view;
        private IRegisterMap _map;
        private Mock<IVimBuffer> _bufferData;
        private Mock<IOperations> _operations;
        private Mock<IEditorOperations> _editorOperations;
        private Mock<IIncrementalSearch> _incrementalSearch;
        private Mock<IJumpList> _jumpList;
        private Mock<IStatusUtil> _statusUtil;
        private Mock<IChangeTracker> _changeTracker;
        private Mock<IDisplayWindowBroker> _displayWindowBroker;

        static string[] s_lines = new string[]
            {
                "summary description for this line",
                "some other line",
                "running out of things to make up"
            };

        public void Create(params string[] lines)
        {
            _view = Utils.EditorUtil.CreateView(lines);
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 0));
            _map = new RegisterMap();
            _editorOperations = new Mock<IEditorOperations>();
            _incrementalSearch = new Mock<IIncrementalSearch>(MockBehavior.Strict);
            _jumpList = new Mock<IJumpList>(MockBehavior.Strict);
            _statusUtil = new Mock<IStatusUtil>(MockBehavior.Strict);
            _changeTracker = new Mock<IChangeTracker>(MockBehavior.Strict);
            _displayWindowBroker = new Mock<IDisplayWindowBroker>(MockBehavior.Strict);
            _displayWindowBroker.SetupGet(x => x.IsSmartTagWindowActive).Returns(false);
            _bufferData = MockFactory.CreateVimBuffer(
                _view,
                "test",
                MockFactory.CreateVim(_map,changeTracker:_changeTracker.Object).Object,
                _jumpList.Object);
            _operations = new Mock<IOperations>(MockBehavior.Strict);
            _operations.SetupGet(x => x.EditorOperations).Returns(_editorOperations.Object);
            _operations.SetupGet(x => x.TextView).Returns(_view);
            _modeRaw = new Vim.Modes.Normal.NormalMode(Tuple.Create(_bufferData.Object, _operations.Object, _incrementalSearch.Object, _statusUtil.Object, _displayWindowBroker.Object));
            _mode = _modeRaw;
            _mode.OnEnter();
        }

        [TearDown]
        public void TearDown()
        {
            _view = null;
            _mode = null;
        }

        [Test]
        public void ModeKindTest()
        {
            Create(s_lines);
            Assert.AreEqual(ModeKind.Normal, _mode.ModeKind);
        }

        [Test, Description("Let enter go straight back to the editor in the default case")]
        public void EnterProcessing()
        {
            Create(s_lines);
            var can = _mode.CanProcess(InputUtil.VimKeyToKeyInput(VimKey.EnterKey));
            Assert.IsTrue(can);
        }

        #region CanProcess

        [Test, Description("Can process basic commands")]
        public void CanProcess1()
        {
            Create(s_lines);
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('u')));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('h')));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('j')));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('i')));
        }

        [Test, Description("Can process even invalid commands else they end up as input")]
        public void CanProcess2()
        {
            Create(s_lines);
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('U')));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('Z')));
        }

        [Test, Description("Must be able to process numbers")]
        public void CanProcess3()
        {
            Create(s_lines);
            foreach (var cur in Enumerable.Range(1, 8))
            {
                var c = char.Parse(cur.ToString());
                var ki = InputUtil.CharToKeyInput(c);
                Assert.IsTrue(_mode.CanProcess(ki));
            }
        }

        [Test, Description("When in a need more state, process everything")]
        public void CanProcess4()
        {
            Create(s_lines);
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap));
            _mode.Process(InputUtil.CharToKeyInput('/'));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('U')));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('Z')));
        }

        [Test, Description("Don't process while a smart tag is open otherwise you prevent it from being used")]
        public void CanProcess5()
        {
            Create(s_lines);
            _displayWindowBroker.SetupGet(x => x.IsSmartTagWindowActive).Returns(true);
            Assert.IsFalse(_mode.CanProcess(InputUtil.VimKeyToKeyInput(VimKey.EnterKey)));
            Assert.IsFalse(_mode.CanProcess(InputUtil.VimKeyToKeyInput(VimKey.LeftKey)));
            Assert.IsFalse(_mode.CanProcess(InputUtil.VimKeyToKeyInput(VimKey.DownKey)));
        }

        [Test,Description("Should be able to handle ever core character")]
        public void CanProcess6()
        {
            Create(s_lines);
            foreach (var cur in InputUtil.CoreCharacters)
            {
                Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput(cur)));
            }
        }

        [Test, Description("Must be able to handle certain movement keys")]
        public void CanProcess7()
        {
            Create(s_lines);
            Assert.IsTrue(_mode.CanProcess(InputUtil.VimKeyToKeyInput(VimKey.EnterKey)));
            Assert.IsTrue(_mode.CanProcess(InputUtil.VimKeyToKeyInput(VimKey.TabKey)));
        }

        #endregion

        #region Movement

        [Test]
        public void Move_l()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(1)).Verifiable();
            _mode.Process("l");
            _operations.Verify();
        }

        [Test]
        public void Move_l2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(2)).Verifiable();
            _mode.Process("2l");
            _operations.Verify();
        }

        [Test]
        public void Move_h()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(1)).Verifiable();
            _mode.Process("h");
            _operations.Verify();
        }

        [Test]
        public void Move_h2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(2)).Verifiable();
            _mode.Process("2h");
            _operations.Verify();
        }

        [Test]
        public void Move_Backspace1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.BackKey));
            _operations.Verify();
        }

        [Test]
        public void Move_Backspace2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.BackKey));
            _operations.Verify();
        }

        [Test]
        public void Move_k()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretUp(1)).Verifiable();
            _mode.Process("k");
            _operations.Verify();
        }

        [Test]
        public void Move_j()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretDown(1)).Verifiable();
            _mode.Process("j");
            _operations.Verify();
        }

        [Test]
        public void Move_LeftArrow1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.LeftKey));
            _operations.Verify();
        }

        [Test]
        public void Move_LeftArrow2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.LeftKey));
            _operations.Verify();
        }

        [Test]
        public void Move_RightArrow1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.RightKey));
            _operations.Verify();
        }

        [Test]
        public void Move_RightArrow2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.RightKey));
            _operations.Verify();
        }

        [Test]
        public void Move_UpArrow1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretUp(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.UpKey));
            _operations.Verify();
        }

        [Test]
        public void Move_UpArrow2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretUp(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.UpKey));
            _operations.Verify();
        }

        [Test]
        public void Move_DownArrow1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretDown(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.DownKey));
            _operations.Verify();
        }

        [Test]
        public void Move_DownArrow2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretDown(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.DownKey));
            _operations.Verify();
        }

        [Test]
        public void Move_CtrlP1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretUp(1)).Verifiable();
            _mode.Process(new KeyInput('p', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Move_CtrlN1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretDown(1)).Verifiable();
            _mode.Process(new KeyInput('n', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Move_CtrlH1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretLeft(1)).Verifiable();
            _mode.Process(new KeyInput('h', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Move_SpaceBar1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(1)).Verifiable();
            _mode.Process(InputUtil.CharToKeyInput(' '));
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_w1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('w');
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_W1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('W');
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_b1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('b');
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_B1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('B');
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_Enter1()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.EnterKey));
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_Enter2()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.EnterKey));
            _operations.Verify();
        }

        [Test]
        public void Move_Motion_Hat1()
        {
            Create("foo bar");
            _view.MoveCaretTo(3);
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('^');
            _editorOperations.Verify();
        }

        [Test]
        public void Move_Motion_Hat2()
        {
            Create("   foo bar");
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('^');
            _editorOperations.Verify();

        }

        [Test]
        public void Move_Motion_Dollar1()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.MoveCaretToMotionData(It.IsAny<MotionData>())).Verifiable();
            _mode.Process('$');
            _editorOperations.Verify();
        }

        [Test]
        public void Move_0()
        {
            Create("foo bar baz");
            _editorOperations.Setup(x => x.MoveToStartOfLine(false)).Verifiable();
            _view.MoveCaretTo(3);
            _mode.Process('0');
            _editorOperations.Verify();
        }

        [Test]
        public void Move_gUnderscore_1()
        {
            Create("foo bar ");
            _editorOperations.Setup(x => x.MoveToLastNonWhiteSpaceCharacter(false)).Verifiable();
            _mode.Process("g_");
            _editorOperations.Verify();
        }

        [Test]
        public void Move_G_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToLineOrLast(FSharpOption<int>.None)).Verifiable();
            _mode.Process('G');
            _operations.Verify();
        }

        [Test]
        public void Move_G_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToLineOrLast(FSharpOption.Create(42))).Verifiable();
            _mode.Process("42G");
            _operations.Verify();
        }

        [Test]
        public void Move_gg_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToLineOrFirst(FSharpOption<int>.None)).Verifiable();
            _mode.Process("gg");
            _operations.Verify();
        }

        [Test]
        public void Move_gg_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToLineOrFirst(FSharpOption.Create(42))).Verifiable();
            _mode.Process("42gg");
            _operations.Verify();
        }

        [Test]
        public void Move_CHome_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToLineOrFirst(FSharpOption<int>.None)).Verifiable();
            _mode.Process(new KeyInput(Char.MinValue, VimKey.HomeKey, KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Move_CHome_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToLineOrFirst(FSharpOption.Create(42))).Verifiable();
            _mode.Process("42");
            _mode.Process(new KeyInput(Char.MinValue, VimKey.HomeKey, KeyModifiers.Control));
            _operations.Verify();
        }

        #endregion

        #region Scroll

        [Test]
        public void ScrollUp1()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.ScrollLines(ScrollDirection.Up, 1)).Verifiable();
            _mode.Process(new KeyInput('u', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test, Description("Don't break at line 0")]
        public void ScrollUp2()
        {
            Create("foo", "bar");
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(0).End);
            _operations.Setup(x => x.ScrollLines(ScrollDirection.Up, 2)).Verifiable();
            _mode.Process('2');
            _mode.Process(new KeyInput('u', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void ScrollDown1()
        {
            Create("foo", "bar");
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(0).End);
            _operations.Setup(x => x.ScrollLines(ScrollDirection.Down, 1)).Verifiable();
            _mode.Process(new KeyInput('d', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Scroll_zEnter()
        {
            Create("foo", "bar");
            _editorOperations.Setup(x => x.ScrollLineTop()).Verifiable();
            _editorOperations.Setup(x => x.MoveToStartOfLineAfterWhiteSpace(false)).Verifiable();
            _mode.Process("z");
            _mode.Process(VimKey.EnterKey);
            _editorOperations.Verify();
        }

        [Test]
        public void ScrollPages1()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Down, 1)).Verifiable();
            _mode.Process(InputUtil.CharAndModifiersToKeyInput('f', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void ScrollPages2()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Down, 2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.CharAndModifiersToKeyInput('f', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void ScollPages3()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Down, 1)).Verifiable();
            _mode.Process(InputUtil.VimKeyAndModifiersToKeyInput(VimKey.DownKey, KeyModifiers.Shift));
            _operations.Verify();
        }

        [Test]
        public void ScrollPages4()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Down, 1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.PageDownKey));
            _operations.Verify();
        }

        [Test]
        public void ScrollPages5()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Up, 1)).Verifiable();
            _mode.Process(InputUtil.CharAndModifiersToKeyInput('b', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void ScrollPages6()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Up, 2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.CharAndModifiersToKeyInput('b', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void ScrollPages7()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Up, 1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.PageUpKey));
            _operations.Verify();
        }

        [Test]
        public void ScrollPages8()
        {
            Create("foo bar");
            _operations.Setup(x => x.ScrollPages(ScrollDirection.Up, 1)).Verifiable();
            _mode.Process(InputUtil.VimKeyAndModifiersToKeyInput(VimKey.UpKey, KeyModifiers.Shift));
            _operations.Verify();
        }

        [Test]
        public void Scroll_zt()
        {
            Create("foo", "bar");
            _editorOperations.Setup(x => x.ScrollLineTop()).Verifiable();
            _mode.Process("zt");
            _editorOperations.Verify();
        }

        [Test]
        public void Scroll_zPeriod()
        {
            Create("foo", "bar");
            _editorOperations.Setup(x => x.ScrollLineCenter()).Verifiable();
            _editorOperations.Setup(x => x.MoveToStartOfLineAfterWhiteSpace(false)).Verifiable();
            _mode.Process("z.");
            _editorOperations.Verify();
        }

        [Test]
        public void Scroll_zz()
        {
            Create("foo", "bar");
            _editorOperations.Setup(x => x.ScrollLineCenter()).Verifiable();
            _mode.Process("z.");
            _editorOperations.Verify();
        }

        [Test]
        public void Scroll_zDash()
        {
            Create(String.Empty);
            _editorOperations.Setup(x => x.ScrollLineBottom()).Verifiable();
            _editorOperations.Setup(x => x.MoveToStartOfLineAfterWhiteSpace(false)).Verifiable();
            _mode.Process("z-");
            _editorOperations.Verify();
        }

        [Test]
        public void Scroll_zb()
        {
            Create(String.Empty);
            _editorOperations.Setup(x => x.ScrollLineBottom()).Verifiable();
            _editorOperations.Setup(x => x.MoveToStartOfLineAfterWhiteSpace(false)).Verifiable();
            _mode.Process("z-");
            _editorOperations.Verify();
        }

        [Test]
        public void Scroll_zInvalid()
        {
            Create(String.Empty);
            _operations.Setup(x => x.Beep()).Verifiable();
            _mode.Process("z;");
            _operations.Verify();
        }

        #endregion

        #region Motion

        [Test, Description("Typing in invalid motion should produce a warning")]
        public void BadMotion1()
        {
            Create(s_lines);
            _statusUtil.Setup(x => x.OnError(It.IsAny<string>())).Verifiable();
            _mode.Process("d@");
            _statusUtil.Verify();
        }

        [Test, Description("Typing in invalid motion should produce a warning")]
        public void BadMotion2()
        {
            Create(s_lines);
            _statusUtil.Setup(x => x.OnError(It.IsAny<string>())).Verifiable();
            _mode.Process("d@aoeuaoeu");
            _statusUtil.Verify();
        }

        [Test, Description("Enter must cancel an invalid motion")]
        public void BadMotion3()
        {
            Create(s_lines);
            _statusUtil.Setup(x => x.OnError(It.IsAny<string>())).Verifiable();
            _mode.Process("d@");
            var res = _mode.Process(InputUtil.CharToKeyInput('i'));
            Assert.IsTrue(res.IsProcessed);
            _mode.Process(VimKey.EnterKey);
            res = _mode.Process(InputUtil.CharToKeyInput('i'));
            Assert.IsTrue(res.IsSwitchMode);
            _statusUtil.Verify();
        }

        [Test]
        public void Motion_l()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(1)).Verifiable();
            _mode.Process("l");
            _operations.Verify();
        }

        [Test]
        public void Motion_2l()
        {
            Create(s_lines);
            _operations.Setup(x => x.MoveCaretRight(2)).Verifiable();
            _mode.Process("2l");
            _operations.Verify();
        }

        [Test]
        public void Motion_50l()
        {
            Create(s_lines);
            var line = _view.TextSnapshot.Lines.Last();
            _view.Caret.MoveTo(line.Start);
            _operations.Setup(x => x.MoveCaretRight(50)).Verifiable();
            _mode.Process("50l");
            _operations.Verify();
        }

        #endregion

        #region Edits

        [Test]
        public void Edit_o_1()
        {
            Create("how is", "foo");
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 0));
            _operations.Setup(x => x.InsertLineBelow()).Returns<ITextSnapshotLine>(null).Verifiable();
            var res = _mode.Process('o');
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test, Description("Use o at end of buffer")]
        public void Edit_o_2()
        {
            Create("foo", "bar");
            var line = _view.TextSnapshot.Lines.Last();
            _view.Caret.MoveTo(line.Start);
            _operations.Setup(x => x.InsertLineBelow()).Returns<ITextSnapshotLine>(null).Verifiable();
            _mode.Process('o');
            _operations.Verify();
        }

        [Test]
        public void Edit_O_1()
        {
            Create("foo");
            _operations.Setup(x => x.InsertLineAbove()).Returns<ITextSnapshotLine>(null).Verifiable();
            _mode.Process('O');
            _operations.Verify();
        }

        [Test]
        public void Edit_O_2()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.InsertLineAbove()).Returns<ITextSnapshotLine>(null).Verifiable();
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(1).Start);
            _mode.Process("O");
            _operations.Verify();
        }

        [Test]
        public void Edit_O_3()
        {
            Create("foo");
            _operations.Setup(x => x.InsertLineAbove()).Returns<ITextSnapshotLine>(null).Verifiable();
            var res = _mode.Process('O');
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().item);
        }

        [Test]
        public void Edit_X_1()
        {
            Create("foo");
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _operations.Setup(x => x.DeleteCharacterBeforeCursor(1, It.IsAny<Register>())).Verifiable();
            _mode.Process("X");
            _operations.Verify();
        }

        [Test, Description("Don't delete past the current line")]
        public void Edit_X_2()
        {
            Create("foo", "bar");
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(1).Start);
            _operations.Setup(x => x.DeleteCharacterBeforeCursor(1, It.IsAny<Register>())).Verifiable();
            _mode.Process("X");
            _operations.Verify();
        }

        [Test]
        public void Edit_2X_1()
        {
            Create("foo", "bar");
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(0).Start.Add(2));
            _operations.Setup(x => x.DeleteCharacterBeforeCursor(2, It.IsAny<Register>())).Verifiable();
            _mode.Process("2X");
            _operations.Verify();
        }

        [Test]
        public void Edit_2X_2()
        {
            Create("foo");
            _operations.Setup(x => x.DeleteCharacterBeforeCursor(2, It.IsAny<Register>())).Verifiable();
            _mode.Process("2X");
            _operations.Verify();
        }

        [Test]
        public void Edit_r_1()
        {
            Create("foo");
            var ki = InputUtil.CharToKeyInput('b');
            _operations.Setup(x => x.ReplaceChar(ki, 1)).Returns(true).Verifiable();
            _mode.Process("rb");
            _operations.Verify();
        }

        [Test]
        public void Edit_r_2()
        {
            Create("foo");
            var ki = InputUtil.CharToKeyInput('b');
            _operations.Setup(x => x.ReplaceChar(ki, 2)).Returns(true).Verifiable();
            _mode.Process("2rb");
            _operations.Verify();
        }

        [Test]
        public void Edit_r_3()
        {
            Create("foo");
            var ki = InputUtil.VimKeyToKeyInput(VimKey.EnterKey);
            _operations.Setup(x => x.ReplaceChar(ki, 1)).Returns(true).Verifiable();
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _mode.Process("r");
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.EnterKey));
            _operations.Verify();
        }

        [Test]
        public void Edit_r_4()
        {
            Create("food");
            _operations.Setup(x => x.Beep()).Verifiable();
            _operations.Setup(x => x.ReplaceChar(It.IsAny<KeyInput>(), 200)).Returns(false).Verifiable();
            _mode.Process("200ru");
            _operations.Verify();
        }

        [Test,Description("Escape should exit replace not be a part of it")]
        public void Edit_r_5()
        {
            Create("foo");
            _mode.Process("200r");
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.EscapeKey));
            Assert.IsFalse(_mode.IsInReplace);
            Assert.IsFalse(_mode.IsWaitingForInput);
        }

        [Test]
        public void Edit_x_1()
        {
            Create("foo");
            _operations.Setup(x => x.DeleteCharacterAtCursor(1, It.IsAny<Register>())).Verifiable();
            _mode.Process("x");
            _operations.Verify();
        }

        [Test]
        public void Edit_2x()
        {
            Create("foo");
            _operations.Setup(x => x.DeleteCharacterAtCursor(2, It.IsAny<Register>())).Verifiable();
            _mode.Process("2x");
            _operations.Verify();
        }

        [Test]
        public void Edit_x_2()
        {
            Create("foo");
            var reg = _map.GetRegister('c');
            _operations.Setup(x => x.DeleteCharacterAtCursor(1, reg)).Verifiable();
            _mode.Process("\"cx");
            _operations.Verify();
        }

        [Test]
        public void Edit_Del_1()
        {
            Create("foo");
            _operations.Setup(x => x.DeleteCharacterAtCursor(1, _map.DefaultRegister)).Verifiable();
            _mode.Process(VimKey.DeleteKey);
            _operations.Verify();
        }

        [Test]
        public void Edit_c_1()
        {
            Create("foo bar");
            _operations
                .Setup(x => x.DeleteSpan(new SnapshotSpan(_view.TextSnapshot, 0, 4), MotionKind.Exclusive, OperationKind.CharacterWise, _map.DefaultRegister))
                .Returns(_view.TextSnapshot)
                .Verifiable();
            var res = _mode.Process("cw");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_c_2()
        {
            Create("foo bar");
            var reg = _map.GetRegister('c');
            _operations
                .Setup(x => x.DeleteSpan(new SnapshotSpan(_view.TextSnapshot, 0, 4), MotionKind.Exclusive, OperationKind.CharacterWise, reg))
                .Returns(_view.TextSnapshot)
                .Verifiable();
            var res = _mode.Process("\"ccw");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_cc_1()
        {
            Create("foo", "bar", "baz");
            _operations
                .Setup(x => x.DeleteSpan(_view.GetLineSpanIncludingLineBreak(0, 0), MotionKind.Inclusive, OperationKind.LineWise, _map.DefaultRegister))
                .Returns(_view.TextSnapshot)
                .Verifiable();
            var res = _mode.Process("cc");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_cc_2()
        {
            Create("foo", "bar", "baz");
            _operations
                .Setup(x => x.DeleteSpan(_view.GetLineSpanIncludingLineBreak(0, 1), MotionKind.Inclusive, OperationKind.LineWise, _map.DefaultRegister))
                .Returns(_view.TextSnapshot)
                .Verifiable();
            var res = _mode.Process("2cc");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_C_1()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLinesFromCursor(1, _map.DefaultRegister)).Verifiable();
            var res = _mode.Process("C");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_C_2()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLinesFromCursor(1, _map.GetRegister('b'))).Verifiable();
            var res = _mode.Process("\"bC");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test, Description("Delete from the cursor")]
        public void Edit_C_3()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLinesFromCursor(2, _map.GetRegister('b'))).Verifiable();
            var res = _mode.Process("\"b2C");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_s_1()
        {
            Create("foo bar");
            _operations.Setup(x => x.DeleteCharacterAtCursor(1, _map.DefaultRegister)).Verifiable();
            var res = _mode.Process("s");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_s_2()
        {
            Create("foo bar");
            _operations.Setup(x => x.DeleteCharacterAtCursor(2, _map.DefaultRegister)).Verifiable();
            var res = _mode.Process("2s");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_s_3()
        {
            Create("foo bar");
            _operations.Setup(x => x.DeleteCharacterAtCursor(1, _map.GetRegister('c'))).Verifiable();
            var res = _mode.Process("\"cs");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_S_1()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLines(1, _map.DefaultRegister)).Verifiable();
            var res = _mode.Process("S");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_S_2()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLines(2, _map.DefaultRegister)).Verifiable();
            var res = _mode.Process("2S");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_S_3()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLines(300, _map.DefaultRegister)).Verifiable();
            var res = _mode.Process("300S");
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void Edit_Tilde1()
        {
            Create("foo");
            _operations.Setup(x => x.ChangeLetterCaseAtCursor(1)).Verifiable();
            _mode.Process("~");
            _operations.Verify();
        }

        [Test]
        public void Edit_Tilde2()
        {
            Create("foo");
            _operations.Setup(x => x.ChangeLetterCaseAtCursor(30)).Verifiable();
            _mode.Process("30~");
            _operations.Verify();
        }

        [Test, Description("When TildeOp is set it becomes a motion command")]
        public void Edit_Tilde3()
        {
            Create("foo");
            _bufferData.Object.Settings.GlobalSettings.TildeOp = true;
            _mode.Process("~");
        }

        [Test]
        public void Edit_Tilde4()
        {
            Create("foo");
            _bufferData.Object.Settings.GlobalSettings.TildeOp = true;
            _operations.Setup(x => x.ChangeLetterCase(_view.TextBuffer.GetLineSpan(0,0))).Verifiable();
            _mode.Process("~aw");
            _operations.Verify();
        }

        #endregion

        #region Yank

        [Test]
        public void Yank_yw()
        {
            Create("foo");
            _operations.Setup(x => x.Yank(
                _view.TextSnapshot.GetLineFromLineNumber(0).Extent,
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yw");
            _operations.Verify();
        }

        [Test, Description("Yanks in the middle of the word should only get a partial")]
        public void Yank_yw_2()
        {
            Create("foo bar baz");
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 1, 3),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yw");
            _operations.Verify();
        }

        [Test, Description("Yank word should go to the start of the next word including spaces")]
        public void Yank_yw_3()
        {
            Create("foo bar");
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0, 4),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yw");
            _operations.Verify();
        }

        [Test, Description("Non-default register")]
        public void Yank_yw_4()
        {
            Create("foo bar");
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0, 4),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.GetRegister('c'))).Verifiable();
            _mode.Process("\"cyw");
            _operations.Verify();
        }

        [Test]
        public void Yank_2yw()
        {
            Create("foo bar baz");
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0, 8),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("2yw");
            _operations.Verify();
        }

        [Test]
        public void Yank_3yw()
        {
            Create("foo bar baz joe");
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0, 12),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("3yw");
            _operations.Verify();
        }

        [Test]
        public void Yank_yaw()
        {
            Create("foo bar");
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0, 4),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yaw");
            _operations.Verify();
        }

        [Test]
        public void Yank_y2w()
        {
            Create("foo bar baz");
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0, 8),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("y2w");
            _operations.Verify();
        }


        [Test]
        public void Yank_yaw_2()
        {
            Create("foo bar");
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _operations.Setup(x => x.Yank(
                new SnapshotSpan(_view.TextSnapshot, 0,4),
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yaw");
            _operations.Verify();
        }

        [Test]
        public void Yank_yaw_3()
        {
            Create(s_lines);
            _mode.Process("ya");
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.EscapeKey));
            Assert.IsFalse(_mode.IsWaitingForInput);
        }

        [Test, Description("A yy should grab the end of line including line break information")]
        public void Yank_yy_1()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.Yank(
                _view.TextSnapshot.GetLineFromLineNumber(0).ExtentIncludingLineBreak,
                MotionKind.Inclusive,
                OperationKind.LineWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yy");
            _operations.Verify();
        }

        [Test, Description("yy should yank the entire line even if the cursor is not at the start")]
        public void Yank_yy_2()
        {
            Create("foo", "bar");
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _operations.Setup(x => x.Yank(
                _view.TextSnapshot.GetLineFromLineNumber(0).ExtentIncludingLineBreak,
                MotionKind.Inclusive,
                OperationKind.LineWise,
                _map.DefaultRegister)).Verifiable();
            _mode.Process("yy");
            _operations.Verify();
        }

        [Test]
        public void Yank_Y_1()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.YankLines(1, _map.DefaultRegister)).Verifiable();
            _mode.Process("Y");
            _operations.Verify();
        }

        [Test]
        public void Yank_Y_2()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.YankLines(1, _map.GetRegister('c'))).Verifiable();
            _mode.Process("\"cY");
            _operations.Verify();
        }

        [Test]
        public void Yank_Y_3()
        {
            Create("foo", "bar", "jazz");
            _operations.Setup(x => x.YankLines(2, _map.DefaultRegister)).Verifiable();
            _mode.Process("2Y");
            _operations.Verify();
        }

        #endregion

        #region Paste

        [Test]
        public void Paste_p()
        {
            Create("foo bar");
            _operations.Setup(x => x.PasteAfterCursor("hey", 1, OperationKind.CharacterWise, false)).Verifiable();
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process('p');
            _operations.Verify();
        }

        [Test, Description("Paste from a non-default register")]
        public void Paste_p_2()
        {
            Create("foo");
            _operations.Setup(x => x.PasteAfterCursor("hey", 1, OperationKind.CharacterWise, false)).Verifiable();
            _map.GetRegister('j').UpdateValue("hey");
            _mode.Process("\"jp");
            _operations.Verify();
        }

        [Test, Description("Pasting a linewise motion should occur on the next line")]
        public void Paste_p_3()
        {
            Create("foo", "bar");
            var data = "baz" + Environment.NewLine;
            _operations.Setup(x => x.PasteAfterCursor(data, 1, OperationKind.LineWise, false)).Verifiable();
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 0));
            _map.DefaultRegister.UpdateValue(new RegisterValue(data, MotionKind.Inclusive, OperationKind.LineWise));
            _mode.Process("p");
            _operations.Verify();
        }

        [Test]
        public void Paste_2p()
        {
            Create("foo");
            _operations.Setup(x => x.PasteAfterCursor("hey", 2, OperationKind.CharacterWise, false)).Verifiable();
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process("2p");
            _operations.Verify();
        }

        [Test]
        public void Paste_P()
        {
            Create("foo");
            _operations.Setup(x => x.PasteBeforeCursor("hey", 1, OperationKind.CharacterWise, false)).Verifiable();
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process('P');
            _operations.Verify();
        }

        [Test, Description("Pasting a linewise motion should occur on the previous line")]
        public void Paste_P_2()
        {
            Create("foo", "bar");
            var data = "baz" + Environment.NewLine;
            _operations.Setup(x => x.PasteBeforeCursor(data, 1, OperationKind.LineWise, false)).Verifiable();
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _map.DefaultRegister.UpdateValue(new RegisterValue(data, MotionKind.Inclusive, OperationKind.LineWise));
            _mode.Process('P');
            _operations.Verify();
        }

        [Test]
        public void Paste_2P()
        {
            Create("foo");
            _operations.Setup(x => x.PasteBeforeCursor("hey", 2, OperationKind.CharacterWise, false)).Verifiable();
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process("2P");
            _operations.Verify();
        }

        [Test]
        public void Paste_gp_1()
        {
            Create("foo");
            _operations.Setup(x => x.PasteAfterCursor("hey", 1, OperationKind.CharacterWise, true)).Verifiable();
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process("gp");
            _operations.Verify();
        }

        [Test]
        public void Paste_gp_2()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.PasteAfterCursor("hey", 1, OperationKind.CharacterWise, true)).Verifiable();
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(0).End);
            _map.GetRegister('c').UpdateValue("hey");
            _mode.Process("\"cgp");
            _operations.Verify();
        }

        [Test]
        public void Paste_gP_1()
        {
            Create("foo");
            _operations.Setup(x => x.PasteBeforeCursor("hey", 1, OperationKind.CharacterWise, true)).Verifiable();
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process("gP");
            _operations.Verify();
        }

        [Test]
        public void Paste_gP_2()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.PasteBeforeCursor("hey", 1, OperationKind.CharacterWise, true)).Verifiable();
            _view.Caret.MoveTo(_view.TextSnapshot.GetLineFromLineNumber(0).End);
            _map.DefaultRegister.UpdateValue("hey");
            _mode.Process("gP");
            _operations.Verify();
        }

        #endregion

        #region Delete

        [Test, Description("Make sure a dd is a linewise action")]
        public void Delete_dd_1()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.DeleteLinesIncludingLineBreak(1, _map.DefaultRegister)).Verifiable();
            _mode.Process("dd");
            _operations.Verify();
        }

        [Test, Description("Make sure that it deletes the entire line regardless of where the caret is")]
        public void Delete_dd_2()
        {
            Create("foo", "bar");
            _view.Caret.MoveTo(new SnapshotPoint(_view.TextSnapshot, 1));
            _operations.Setup(x => x.DeleteLinesIncludingLineBreak(1, _map.DefaultRegister)).Verifiable();
            _mode.Process("dd");
            _operations.Verify();
        }

        [Test]
        public void Delete_dd_3()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.DeleteLinesIncludingLineBreak(2, _map.DefaultRegister)).Verifiable();
            _mode.Process("2dd");
            _operations.Verify();
        }

        [Test]
        public void Delete_dw_1()
        {
            Create("foo bar baz");
            _operations.Setup(x => x.DeleteSpan(
                new SnapshotSpan(_view.TextSnapshot, 0, 4),
                MotionKind._unique_Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister))
                .Returns(It.IsAny<ITextSnapshot>())
                .Verifiable();
            _mode.Process("dw");
            _operations.Verify();
        }

        [Test, Description("Delete at the end of the line shouldn't delete newline")]
        public void Delete_dw_2()
        {
            Create("foo bar", "baz");
            var point = new SnapshotPoint(_view.TextSnapshot, 4);
            _view.Caret.MoveTo(point);
            Assert.AreEqual('b', _view.Caret.Position.BufferPosition.GetChar());
            var span = new SnapshotSpan(point, _view.TextSnapshot.GetLineFromLineNumber(0).End);
            _operations.Setup(x => x.DeleteSpan(
                span,
                MotionKind.Exclusive,
                OperationKind.CharacterWise,
                _map.DefaultRegister))
                .Returns(It.IsAny<ITextSnapshot>())
                .Verifiable();
            _mode.Process("dw");
            _operations.Verify();
        }

        [Test, Description("Escape should exit d")]
        public void Delete_d_1()
        {
            Create(s_lines);
            _mode.Process('d');
            Assert.IsTrue(_mode.IsWaitingForInput);
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.EscapeKey));
            Assert.IsFalse(_mode.IsWaitingForInput);
        }

        [Test]
        public void Delete_D_1()
        {
            Create("foo bar");
            _operations.Setup(x => x.DeleteLinesFromCursor(1, _map.DefaultRegister)).Verifiable();
            _mode.Process("D");
            _operations.Verify();
        }

        [Test]
        public void Delete_D_2()
        {
            Create("foo bar baz");
            _operations.Setup(x => x.DeleteLinesFromCursor(1, _map.GetRegister('b'))).Verifiable();
            _mode.Process("\"bD");
            _operations.Verify();
        }

        [Test]
        public void Delete_D_3()
        {
            Create("foo bar");
            _operations.Setup(x => x.DeleteLinesFromCursor(3, _map.DefaultRegister)).Verifiable();
            _mode.Process("3D");
            _operations.Verify();
        }

        #endregion

        #region Regressions

        [Test, Description("Don't re-enter insert mode on every keystroke once you've left")]
        public void Regression_InsertMode()
        {
            Create(s_lines);
            var res = _mode.Process('i');
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _operations.Setup(x => x.MoveCaretLeft(1)).Verifiable();
            res = _mode.Process('h');
            Assert.IsTrue(res.IsProcessed);
            _operations.Verify();
        }

        [Test, Description("j past the end of the buffer")]
        public void Regression_DownPastBufferEnd()
        {
            Create("foo");
            _operations.Setup(x => x.MoveCaretDown(1)).Verifiable();
            var res = _mode.Process('j');
            Assert.IsTrue(res.IsProcessed);
            res = _mode.Process('j');
            Assert.IsTrue(res.IsProcessed);
            _operations.Verify();
        }

        #endregion

        #region Incremental Search

        [Test]
        public void IncrementalSearch1()
        {
            Create("foo bar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap)).Verifiable();
            _mode.Process('/');
            _incrementalSearch.Verify();
        }

        [Test]
        public void IncrementalSearch2()
        {
            Create("foo bar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.BackwardWithWrap)).Verifiable();
            _mode.Process('?');
            _incrementalSearch.Verify();
        }

        [Test]
        public void IncrementalSearch3()
        {
            Create("foo bar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap)).Verifiable();
            _jumpList.Setup(x => x.Add(_view.GetCaretPoint())).Verifiable();
            _mode.Process('/');
            _incrementalSearch.Setup(x => x.Process(It.IsAny<KeyInput>())).Returns(SearchProcessResult.SearchComplete).Verifiable();
            _mode.Process('b');
            _incrementalSearch.Verify();
            _jumpList.Verify();
        }

        [Test, Description("Make sure any key goes to incremental search")]
        public void IncrementalSearch4()
        {
            Create("foo bar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap)).Verifiable();
            _mode.Process('/');
            var ki = InputUtil.CharToKeyInput((char)7);
            _incrementalSearch.Setup(x => x.Process(It.IsAny<KeyInput>())).Returns(SearchProcessResult.SearchComplete).Verifiable();
            _jumpList.Setup(x => x.Add(_view.GetCaretPoint())).Verifiable();
            _mode.Process(ki);
            _incrementalSearch.Verify();
            _jumpList.Verify();
        }

        [Test, Description("After a true return incremental search should be completed")]
        public void IncrementalSearch5()
        {
            Create("foo bar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap)).Verifiable();
            _mode.Process('/');
            var ki = InputUtil.CharToKeyInput('c');
            _incrementalSearch.Setup(x => x.Process(It.IsAny<KeyInput>())).Returns(SearchProcessResult.SearchComplete).Verifiable();
            _jumpList.Setup(x => x.Add(_view.GetCaretPoint())).Verifiable();
            _mode.Process(ki);
            _incrementalSearch.Verify();
            _jumpList.Verify();
        }

        [Test, Description("Cancel should not add to the jump list")]
        public void IncrementalSearch6()
        {
            Create("foo bar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap)).Verifiable();
            _mode.Process('/');
            _incrementalSearch.Setup(x => x.Process(It.IsAny<KeyInput>())).Returns(SearchProcessResult.SearchCancelled).Verifiable();
            _mode.Process(InputUtil.CharToKeyInput((char)8));
            _incrementalSearch.Verify();
            _jumpList.Verify();
        }

        #endregion

        #region Next / Previous Word

        [Test]
        public void NextWord1()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveToNextOccuranceOfWordAtCursor(SearchKind.ForwardWithWrap, 1)).Verifiable();
            _mode.Process("*");
            _operations.Verify();
        }

        [Test, Description("No matches should have no effect")]
        public void NextWord2()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveToNextOccuranceOfWordAtCursor(SearchKind.ForwardWithWrap, 4)).Verifiable();
            _mode.Process("4*");
            _operations.Verify();
        }

        [Test]
        public void PreviousWord1()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveToNextOccuranceOfWordAtCursor(SearchKind.BackwardWithWrap, 1)).Verifiable();
            _mode.Process("#");
            _operations.Verify();
        }

        [Test]
        public void PreviousWord2()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveToNextOccuranceOfWordAtCursor(SearchKind.BackwardWithWrap, 4)).Verifiable();
            _mode.Process("4#");
            _operations.Verify();
        }

        [Test]
        public void NextPartialWord1()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveToNextOccuranceOfPartialWordAtCursor(SearchKind.ForwardWithWrap, 1)).Verifiable();
            _mode.Process("g*");
            _operations.Verify();
        }

        [Test]
        public void PreviousPartialWord1()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveToNextOccuranceOfPartialWordAtCursor(SearchKind.BackwardWithWrap, 1)).Verifiable();
            _mode.Process("g#");
            _operations.Verify();
        }

        #endregion

        #region Search

        [Test]
        public void Search_n_1()
        {
            Create("foo");
            _operations.Setup(x => x.MoveToNextOccuranceOfLastSearch(1, false)).Verifiable();
            _mode.Process("n");
            _operations.Verify();
        }

        [Test]
        public void Search_n_2()
        {
            Create("foo");
            _operations.Setup(x => x.MoveToNextOccuranceOfLastSearch(2, false)).Verifiable();
            _mode.Process("2n");
            _operations.Verify();
        }

        [Test]
        public void Search_N_1()
        {
            Create("foo");
            _operations.Setup(x => x.MoveToNextOccuranceOfLastSearch(1, true)).Verifiable();
            _mode.Process("N");
            _operations.Verify();
        }

        [Test]
        public void Search_N_2()
        {
            Create("foo");
            _operations.Setup(x => x.MoveToNextOccuranceOfLastSearch(2, true)).Verifiable();
            _mode.Process("2N");
            _operations.Verify();
        }

        #endregion

        #region Shift

        [Test]
        public void ShiftRight1()
        {
            Create("foo");
            _operations
                .Setup(x => x.ShiftLinesRight(1))
                .Verifiable();
            _mode.Process(">>");
            _operations.Verify();
        }

        [Test, Description("With a count")]
        public void ShiftRight2()
        {
            Create("foo", "bar");
            var tss = _view.TextSnapshot;
            _operations
                .Setup(x => x.ShiftLinesRight(2))
                .Verifiable();
            _mode.Process("2>>");
            _operations.Verify();
        }

        [Test, Description("With a motion")]
        public void ShiftRight3()
        {
            Create("foo", "bar");
            var tss = _view.TextSnapshot;
            var span = new SnapshotSpan(
                tss.GetLineFromLineNumber(0).Start,
                tss.GetLineFromLineNumber(1).End);
            _operations
                .Setup(x => x.ShiftSpanRight(span))
                .Verifiable();
            _mode.Process(">j");
            _operations.Verify();
        }

        [Test]
        public void ShiftLeft1()
        {
            Create("foo");
            _operations
                .Setup(x => x.ShiftLinesLeft(1))
                .Verifiable();
            _mode.Process("<<");
            _operations.Verify();
        }

        [Test]
        public void ShiftLeft2()
        {
            Create(" foo");
            _operations
                .Setup(x => x.ShiftLinesLeft(1))
                .Verifiable();
            _mode.Process("<<");
            _operations.Verify();
        }

        [Test, Description("With a count")]
        public void ShiftLeft3()
        {
            Create("     foo", "     bar");
            var tss = _view.TextSnapshot;
            _operations
                .Setup(x => x.ShiftLinesLeft(2))
                .Verifiable();
            _mode.Process("2<<");
            _operations.Verify();
        }

        #endregion

        #region Misc

        [Test]
        public void Register1()
        {
            Create("foo");
            Assert.AreEqual('_', _modeRaw.Register.Name);
            _mode.Process("\"c");
            Assert.AreEqual('c', _modeRaw.Register.Name);
        }

        [Test]
        public void Undo1()
        {
            Create("foo");
            _operations.Setup(x => x.Undo(1)).Verifiable();
            _mode.Process("u");
            _operations.Verify();
        }

        [Test]
        public void Undo2()
        {
            Create("foo");
            _operations.Setup(x => x.Undo(2)).Verifiable();
            _mode.Process("2u");
            _operations.Verify();
        }

        [Test]
        public void Redo1()
        {
            Create("foo");
            _operations.Setup(x => x.Redo(1)).Verifiable();
            _mode.Process(new KeyInput('r', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Redo2()
        {
            Create("bar");
            _operations.Setup(x => x.Redo(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(new KeyInput('r', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Join1()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.JoinAtCaret(1)).Verifiable();
            _mode.Process("J");
            _operations.Verify();
        }

        [Test]
        public void Join2()
        {
            Create("foo", "  bar", "baz");
            _operations.Setup(x => x.JoinAtCaret(2)).Verifiable();
            _mode.Process("2J");
            _operations.Verify();
        }

        [Test]
        public void Join3()
        {
            Create("foo", "  bar", "baz");
            _operations.Setup(x => x.JoinAtCaret(3)).Verifiable();
            _mode.Process("3J");
            _operations.Verify();
        }

        [Test]
        public void Join4()
        {
            Create("foo", "bar");
            _operations.Setup(x => x.Join(
                _view.Caret.Position.BufferPosition,
                JoinKind.KeepEmptySpaces,
                1))
                .Returns(true)
                .Verifiable();
            _mode.Process("gJ");
            _operations.Verify();
        }

        [Test]
        public void GoToDefinition1()
        {
            var def = new KeyInput(']', KeyModifiers.Control);
            Create("foo");
            _operations.Setup(x => x.GoToDefinitionWrapper()).Verifiable();
            _mode.Process(def);
            _operations.Verify();
        }

        [Test]
        public void GoToDefinition2()
        {
            Create(s_lines);
            var def = new KeyInput(']', KeyModifiers.Control);
            Assert.IsTrue(_mode.CanProcess(def));
            Assert.IsTrue(_mode.Commands.Contains(def));
        }

        [Test]
        public void GoToMatch1()
        {
            Create("foo bar");
            _operations.Setup(x => x.GoToMatch()).Returns(true);
            Assert.IsTrue(_mode.Process(InputUtil.CharToKeyInput('%')).IsProcessed);
            _operations.Verify();
        }

        [Test]
        public void Mark1()
        {
            Create(s_lines);
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('m')));
            Assert.IsTrue(_mode.Commands.Contains(InputUtil.CharToKeyInput('m')));
        }

        [Test, Description("Once we are in mark mode we can process anything")]
        public void Mark2()
        {
            Create(s_lines);
            _mode.Process(InputUtil.CharToKeyInput('m'));
            Assert.IsTrue(_mode.CanProcess(new KeyInput('c', KeyModifiers.Control)));
        }

        [Test]
        public void Mark3()
        {
            Create(s_lines);
            _operations.Setup(x => x.SetMark(_bufferData.Object, _view.Caret.Position.BufferPosition, 'a')).Returns(Result._unique_Succeeded).Verifiable();
            _mode.Process(InputUtil.CharToKeyInput('m'));
            _mode.Process(InputUtil.CharToKeyInput('a'));
            _operations.Verify();
        }

        [Test, Description("Bad mark should beep")]
        public void Mark4()
        {
            Create(s_lines);
            _operations.Setup(x => x.Beep()).Verifiable();
            _operations.Setup(x => x.SetMark(_bufferData.Object, _view.Caret.Position.BufferPosition, ';')).Returns(Result.NewFailed("foo")).Verifiable();
            _mode.Process(InputUtil.CharToKeyInput('m'));
            _mode.Process(InputUtil.CharToKeyInput(';'));
            _operations.Verify();
        }

        [Test]
        public void JumpToMark1()
        {
            Create(s_lines);
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('\'')));
            Assert.IsTrue(_mode.CanProcess(InputUtil.CharToKeyInput('`')));
            Assert.IsTrue(_mode.Commands.Contains(InputUtil.CharToKeyInput('\'')));
            Assert.IsTrue(_mode.Commands.Contains(InputUtil.CharToKeyInput('`')));
        }

        [Test]
        public void JumpToMark2()
        {
            Create("foobar");
            _operations
                .Setup(x => x.JumpToMark('a', _bufferData.Object.MarkMap))
                .Returns(Result._unique_Succeeded)
                .Verifiable();
            _mode.Process('\'');
            _mode.Process('a');
            _operations.Verify();
        }

        [Test]
        public void JumpToMark3()
        {
            Create("foobar");
            _operations
                .Setup(x => x.JumpToMark('a', _bufferData.Object.MarkMap))
                .Returns(Result._unique_Succeeded)
                .Verifiable();
            _mode.Process('`');
            _mode.Process('a');
            _operations.Verify();
        }

        [Test]
        public void JumpNext1()
        {
            Create(s_lines);
            _operations.Setup(x => x.JumpNext(1)).Verifiable();
            _mode.Process(new KeyInput('i', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void JumpNext2()
        {
            Create(s_lines);
            _operations.Setup(x => x.JumpNext(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(new KeyInput('i', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void JumpNext3()
        {
            Create(s_lines);
            _operations.Setup(x => x.JumpNext(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyToKeyInput(VimKey.TabKey));
            _operations.Verify();
        }

        [Test]
        public void JumpPrevious1()
        {
            Create(s_lines);
            _operations.Setup(x => x.JumpPrevious(1)).Verifiable();
            _mode.Process(new KeyInput('o', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void JumpPrevious2()
        {
            Create(s_lines);
            _operations.Setup(x => x.JumpPrevious(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(new KeyInput('o', KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void Append1()
        {
            Create("foo bar");
            _operations.Setup(x => x.MoveCaretRight(1)).Verifiable();
            var ret = _mode.Process('a');
            Assert.IsTrue(ret.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, ret.AsSwitchMode().Item);
            _operations.Verify();
        }

        [Test]
        public void IsOperatorPending1()
        {
            Create("foobar");
            Assert.IsFalse(_mode.IsOperatorPending);
        }

        [Test]
        public void IsOperatorPending2()
        {
            Create("foobar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap));
            _mode.Process('/');
            Assert.IsFalse(_mode.IsOperatorPending);
        }

        [Test]
        public void IsOperatorPending3()
        {
            Create("");
            _mode.Process('y');
            Assert.IsTrue(_mode.IsOperatorPending);
        }

        [Test]
        public void IsOperatorPending4()
        {
            Create("");
            _mode.Process('d');
            Assert.IsTrue(_mode.IsOperatorPending);
        }

        [Test]
        public void IsWaitingForInput1()
        {
            Create("foobar");
            Assert.IsFalse(_mode.IsWaitingForInput);
        }

        [Test]
        public void IsWaitingForInput2()
        {
            Create("foobar");
            _incrementalSearch.Setup(x => x.Begin(SearchKind.ForwardWithWrap));
            _mode.Process('/');
            Assert.IsTrue(_mode.IsWaitingForInput);
        }

        [Test]
        public void IsWaitingForInput3()
        {
            Create("");
            _mode.Process('y');
            Assert.IsTrue(_mode.IsWaitingForInput);
        }

        [Test]
        public void Command1()
        {
            Create("foo");
            _mode.Process("\"a");
            Assert.AreEqual("\"a", _modeRaw.Command);
        }

        [Test]
        public void Command2()
        {
            Create("bar");
            _mode.Process("\"f");
            Assert.AreEqual("\"f", _modeRaw.Command);
        }

        [Test]
        public void Command3()
        {
            Create("again");
            _operations.Setup(x => x.MoveCaretUp(1));
            _mode.Process('k');
            Assert.AreEqual(string.Empty, _mode.Command);
        }

        [Test]
        public void Command4()
        {
            Create(s_lines);
            _mode.Process('2');
            Assert.AreEqual("2", _mode.Command);
        }

        [Test]
        public void Command5()
        {
            Create(s_lines);
            _mode.Process("2d");
            Assert.AreEqual("2d", _mode.Command);
        }

        [Test]
        public void CommandExecuted1()
        {
            Create("foo");
            _operations.Setup(x => x.DeleteLinesFromCursor(1, _map.DefaultRegister));
            var didSee = false;
            _mode.CommandExecuted += (unused, command) =>
                {
                    Assert.IsTrue(command.IsRepeatableCommand);
                    var com = command.AsRepeatabelCommand();
                    Assert.AreEqual(1, com.Item2);
                    var inputs = com.Item1.ToList();
                    Assert.AreEqual(1, inputs.Count);
                    Assert.AreEqual(InputUtil.CharToKeyInput('D'), inputs[0]);
                    didSee = true;
                };
            _mode.Process('D');
            Assert.IsTrue(didSee);
        }

        [Test]
        public void CommandExecuted2()
        {
            Create("foo");
            _operations.Setup(x => x.DeleteLinesFromCursor(2, _map.DefaultRegister));
            var didSee = false;
            _mode.CommandExecuted += (unused, command) =>
                {
                    Assert.IsTrue(command.IsRepeatableCommand);
                    var com = command.AsRepeatabelCommand();
                    Assert.AreEqual(2, com.Item2);
                    var inputs = com.Item1.ToList();
                    Assert.AreEqual(1, inputs.Count);
                    Assert.AreEqual(InputUtil.CharToKeyInput('D'), inputs[0]);
                    didSee = true;
                };
            _mode.Process("2D");
            Assert.IsTrue(didSee);
        }

        [Test]
        public void CommandExecuted3()
        {
            Create("foo");
            _operations.Setup(x => x.Beep()).Verifiable();
            var didSee = false;
            _mode.CommandExecuted += (unused, command) =>
                {
                    Assert.IsTrue(command.IsNonRepeatableCommand);
                    didSee = true;
                };
            _mode.Process(";");
            Assert.IsTrue(didSee);
            _operations.Verify();
        }

        [Test, Description("Make sure movement keys don't register as executed commands")]
        public void CommandExecuted4()
        {
            Create("foo");
            _operations.Setup(x => x.MoveCaretLeft(1));
            var didSee = false;
            _mode.CommandExecuted += (unused, command) =>
                {
                    didSee = true;
                };
            _mode.Process('h');
            Assert.IsFalse(didSee);
        }

        [Test]
        public void CommandExecuted5()
        {
            Create(s_lines);
            _operations.Setup(x => x.DeleteLinesIncludingLineBreak(2, _map.DefaultRegister));
            var didSee = false;
            _mode.CommandExecuted += (unused, command) =>
                {
                    Assert.IsTrue(command.IsRepeatableCommand);
                    var com = command.AsRepeatabelCommand();
                    Assert.AreEqual(2, com.Item2);
                    var inputs = com.Item1.ToList();
                    Assert.AreEqual(2, inputs.Count);
                    Assert.AreEqual(InputUtil.CharToKeyInput('d'), inputs[0]);
                    Assert.AreEqual(InputUtil.CharToKeyInput('d'), inputs[1]);
                    didSee = true;
                };
            _mode.Process("2dd");
            Assert.IsTrue(didSee);
        }

        private void AssertIsRepeatable(string initialCommand, string repeatCommand = null, int? count = null)
        {
            repeatCommand = repeatCommand ?? initialCommand;
            count = count ?? 1;
            var didSee = false;
            _mode.CommandExecuted += (unused, command) =>
                {
                    Assert.IsTrue(command.IsRepeatableCommand);
                    var com = command.AsRepeatabelCommand();
                    var data = new string(com.Item1.Select(x => x.Char).ToArray());
                    Assert.AreEqual(repeatCommand, data);
                    Assert.AreEqual(count.Value, com.Item2);
                    didSee = true;
                };
            _mode.Process(initialCommand);
            Assert.IsTrue(didSee);
        }

        [Test]
        public void CommandExecute4()
        {
            Create("foo", "bar", "baz");
            _operations.Setup(x => x.DeleteLinesIncludingLineBreak(1, _map.DefaultRegister));
            AssertIsRepeatable("dd");
        }

        [Test]
        public void CommandExecute5()
        {
            Create("foo", "bar", "baz");
            _operations
                .Setup(x => x.DeleteSpan(It.IsAny<SnapshotSpan>(), It.IsAny<MotionKind>(), It.IsAny<OperationKind>(), _map.DefaultRegister))
                .Returns<ITextSnapshot>(null);
            AssertIsRepeatable("d$");
        }

        [Test]
        public void RepeatLastChange1()
        {
            Create("foo");
            _operations.Setup(x => x.Beep()).Verifiable();
            _changeTracker.SetupGet(x => x.LastChange).Returns(FSharpOption<RepeatableChange>.None).Verifiable();
            _mode.Process('.');
            _changeTracker.Verify();
            _operations.Verify();
        }

        [Test]
        public void RepeatLastChange2()
        {
            Create("");
            _changeTracker.SetupGet(x => x.LastChange).Returns(FSharpOption.Create(RepeatableChange.NewTextChange("h"))).Verifiable();
            _operations.Setup(x => x.InsertText("h", 1)).Returns(_view.TextSnapshot).Verifiable();
            _mode.Process('.');
            _operations.Verify();
            _changeTracker.Verify();
        }

        [Test]
        public void RepeatLastChange3()
        {
            Create("");
            _changeTracker.SetupGet(x => x.LastChange).Returns(FSharpOption.Create(RepeatableChange.NewTextChange("h"))).Verifiable();
            _operations.Setup(x => x.InsertText("h", 3)).Returns(_view.TextSnapshot).Verifiable();
            _mode.Process("3.");
            _operations.Verify();
            _changeTracker.Verify();
        }

        [Test]
        public void RepeatLastChange4()
        {
            Create("");
            _changeTracker
                .SetupGet(x => x.LastChange)
                .Returns(FSharpOption.Create(RepeatableChange.NewNormalModeChange(
                    (new KeyInput[] { InputUtil.CharToKeyInput('h')}).ToFSharpList(),
                    1,
                    new Register('c'))))
                .Verifiable();
            _operations.Setup(x => x.MoveCaretLeft(1)).Verifiable();
            _mode.Process(".");
            _operations.Verify();
            _changeTracker.Verify();
        }

        [Test]
        public void RepeatLastChange5()
        {
            Create("");
            _changeTracker
                .SetupGet(x => x.LastChange)
                .Returns(FSharpOption.Create(RepeatableChange.NewNormalModeChange(
                    (new KeyInput[] { InputUtil.CharToKeyInput('h')}).ToFSharpList(),
                    3,
                    new Register('c'))))
                .Verifiable();
            _operations.Setup(x => x.MoveCaretLeft(3)).Verifiable();
            _mode.Process(".");
            _operations.Verify();
            _changeTracker.Verify();
        }

        [Test]
        public void RepeatLastChange6()
        {
            Create("");
            _changeTracker
                .SetupGet(x => x.LastChange)
                .Returns(FSharpOption.Create(RepeatableChange.NewNormalModeChange(
                    (new KeyInput[] { InputUtil.CharToKeyInput('h')}).ToFSharpList(),
                    3,
                    new Register('c'))))
                .Verifiable();
            _operations.Setup(x => x.MoveCaretLeft(2)).Verifiable();
            _mode.Process("2.");
            _operations.Verify();
            _changeTracker.Verify();
        }

        [Test, Description("Executing . should not clear the last command")]
        public void RepeatLastChange7()
        {
            Create("");
            var count = 0;
            _changeTracker
                .SetupGet(x => x.LastChange)
                .Returns(FSharpOption.Create(RepeatableChange.NewNormalModeChange(
                    (new KeyInput[] { InputUtil.CharToKeyInput('h') }).ToFSharpList(),
                    3,
                    new Register('c'))))
                .Callback(() => { count++; });

            _operations.Setup(x => x.MoveCaretLeft(3)).Verifiable();
            _mode.Process(".");
            _mode.Process(".");
            _mode.Process(".");
            _operations.Verify();
            _changeTracker.Verify();
            Assert.AreEqual(3, count);
        }

        [Test, Description("Guard against a possible stack overflow with a recursive . repeat")]
        public void RepeatLastChange8()
        {
            Create("");
            _changeTracker
                .SetupGet(x => x.LastChange)
                .Returns(FSharpOption.Create(RepeatableChange.NewNormalModeChange(
                    (new KeyInput[] { InputUtil.CharToKeyInput('.') }).ToFSharpList(),
                    3,
                    new Register('c'))))
                .Verifiable();

            _statusUtil.Setup(x => x.OnError(Resources.NormalMode_RecursiveRepeatDetected)).Verifiable();
            _mode.Process(".");
            _statusUtil.Verify();
            _changeTracker.Verify();
        }

        #endregion

        #region Visual Mode

        [Test]
        public void VisualMode1()
        {
            Create(s_lines);
            var res = _mode.Process('v');
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.VisualCharacter, res.AsSwitchMode().Item);
        }

        [Test]
        public void VisualMode2()
        {
            Create(s_lines);
            var res = _mode.Process('V');
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.VisualLine, res.AsSwitchMode().Item);
        }

        [Test]
        public void VisualMode3()
        {
            Create(s_lines);
            var res = _mode.Process(new KeyInput('q', KeyModifiers.Control));
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.VisualBlock, res.AsSwitchMode().Item);
        }

        [Test]
        public void ShiftI_1()
        {
            Create(s_lines);
            _editorOperations.Setup(x => x.MoveToStartOfLineAfterWhiteSpace(false)).Verifiable();
            var res = _mode.Process('I');
            Assert.IsTrue(res.IsSwitchMode);
            Assert.AreEqual(ModeKind.Insert, res.AsSwitchMode().Item);
            _editorOperations.Verify();
        }

        [Test]
        public void gt_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToNextTab(1)).Verifiable();
            _mode.Process("gt");
            _operations.Verify();
        }

        [Test]
        public void gt_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToNextTab(2)).Verifiable();
            _mode.Process("2gt");
            _operations.Verify();
        }

        [Test]
        public void CPageDown_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToNextTab(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyAndModifiersToKeyInput(VimKey.PageDownKey, KeyModifiers.Control));
            _operations.Verify();
        }
       
        [Test]
        public void CPageDown_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToNextTab(2)).Verifiable();
            _mode.Process("2");
            _mode.Process(InputUtil.VimKeyAndModifiersToKeyInput(VimKey.PageDownKey, KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void gT_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToPreviousTab(1)).Verifiable();
            _mode.Process("gT");
            _operations.Verify();
        }

        [Test]
        public void gT_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToPreviousTab(2)).Verifiable();
            _mode.Process("2gT");
            _operations.Verify();
        }

        [Test]
        public void CPageUp_1()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToPreviousTab(1)).Verifiable();
            _mode.Process(InputUtil.VimKeyAndModifiersToKeyInput(VimKey.PageUpKey, KeyModifiers.Control));
            _operations.Verify();
        }

        [Test]
        public void CPageUp_2()
        {
            Create(s_lines);
            _operations.Setup(x => x.GoToPreviousTab(2)).Verifiable();
            _mode.Process('2');
            _mode.Process(InputUtil.VimKeyAndModifiersToKeyInput(VimKey.PageUpKey, KeyModifiers.Control));
            _operations.Verify();
        }

        #endregion

    }
}
