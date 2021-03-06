﻿#light


namespace Vim

module internal StringUtil =

    [<CompiledName("Empty")>]
    let empty = System.String.Empty

    [<CompiledName("FindFirst")>]
    let findFirst (input:seq<char>) index del =
        let found = 
            input 
                |> Seq.mapi ( fun i c -> (i,c) )
                |> Seq.skip index 
                |> Seq.skipWhile (fun (i,c) -> not (del c))
        match Seq.isEmpty found with
            | true -> None
            | false -> Some (Seq.head found)
            
    [<CompiledName("isValidIndex")>]
    let isValidIndex index (input:string) = index >= 0 && index < input.Length
            
    [<CompiledName("CharAtOption")>]
    let charAtOption index (input:string) = 
        match isValidIndex index input with
            | true -> Some input.[index]
            | false -> None
            
    [<CompiledName("CharAt")>]
    let charAt index input =
        match charAtOption index input with 
            | Some c -> c
            | None -> failwith "Invalid index"
    
    [<CompiledName("Repeat")>]
    let repeat (value:string) count =
        if 1 = count then value
        else
            let buffer = new System.Text.StringBuilder()
            for i = 1 to count do
                buffer.Append(value) |> ignore
            buffer.ToString()

    /// Create a String from an array of chars
    [<CompiledName("OfCharArray")>]
    let ofCharArray (chars:char[]) = new System.String(chars)

    /// Create a String from a sequence of chars
    [<CompiledName("OfCharSeq")>]
    let ofCharSeq (chars : char seq) = chars |> Array.ofSeq |> ofCharArray

    [<CompiledName("OfCharList")>]
    let ofCharList (chars :char list) = chars |> Seq.ofList |> ofCharSeq

    /// Create a String from a single char
    [<CompiledName("OfChar")>]
    let ofChar c = System.String(c,1)

    [<CompiledName("IsNullOrEmpty")>]
    let isNullOrEmpty str = System.String.IsNullOrEmpty(str)

    [<CompiledName("length")>]
    let length (str:string) = 
        if str = null then 0
        else str.Length

    [<CompiledName("IsEqualIgnoreCase")>]
    let isEqualIgnoreCase left right = 
        let comp = System.StringComparer.OrdinalIgnoreCase
        comp.Equals(left,right)

    [<CompiledName("IsEqual")>]
    let isEqual left right = 
        let comp = System.StringComparer.Ordinal
        comp.Equals(left,right)
