﻿#light

namespace Vim.Modes.Insert
open Vim
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Operations
open Microsoft.VisualStudio.Text.Editor

type internal InsertMode
    ( 
        _data : IVimBuffer, 
        _operations : Modes.ICommonOperations,
        _broker : IDisplayWindowBroker) =
    let _commands = [
        InputUtil.VimKeyToKeyInput VimKey.EscapeKey;
        KeyInput('d', KeyModifiers.Control); ]

    /// Process the CTRL-D combination and do a shift left
    member private this.ShiftLeft() = _operations.ShiftLinesLeft 1

    member private this.ProcessEscape() =
        if _broker.IsCompletionWindowActive then
            _broker.DismissCompletionWindow()

            if _data.Settings.GlobalSettings.DoubleEscape then ProcessResult.Processed
            else ProcessResult.SwitchMode ModeKind.Normal

        else
            ProcessResult.SwitchMode ModeKind.Normal

    interface IMode with 
        member x.VimBuffer = _data
        member x.Commands = _commands |> Seq.ofList
        member x.ModeKind = ModeKind.Insert
        member x.CanProcess (ki:KeyInput) = 
            match _commands |> List.tryFind (fun d -> d = ki) with
            | Some _ -> true
            | None -> false
        member x.Process (ki : KeyInput) = 
            if ki = InputUtil.VimKeyToKeyInput(VimKey.EscapeKey) then x.ProcessEscape()
            elif ki = KeyInput('d', KeyModifiers.Control) then 
                x.ShiftLeft()
                ProcessResult.Processed
            else Processed
        member x.OnEnter () = ()
        member x.OnLeave () = 
            // When leaving insert mode the caret should move one to the left on the
            // same line
            _operations.MoveCaretLeft 1 
            
