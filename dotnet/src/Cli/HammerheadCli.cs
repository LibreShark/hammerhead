﻿using System.CommandLine;
using Google.Protobuf;
using LibreShark.Hammerhead.Api;
using LibreShark.Hammerhead.Codecs;

namespace LibreShark.Hammerhead.Cli;

// ReSharper disable BuiltInTypeReferenceStyle
using u8 = Byte;
using s8 = SByte;
using s16 = Int16;
using u16 = UInt16;
using s32 = Int32;
using u32 = UInt32;
using s64 = Int64;
using u64 = UInt64;
using f64 = Double;

public class HammerheadCli
{
    private readonly string[] _args;
    private readonly HammerheadApi _api;
    private ICliPrinter _printer = new TerminalPrinter();

    public HammerheadCli(string[] args)
    {
        _args = args;
        _api = new HammerheadApi();
    }

    public async Task<int> InvokeAsync()
    {
        var cli = new CliCmd();
        cli.Always += (_, cmdParams) => PrintBanner(cmdParams);
        cli.OnInfo += (_, cmdParams) => PrintFileInfo(cmdParams);
        cli.OnEncryptRom += (_, cmdParams) => _api.EncryptRom(cmdParams);
        cli.OnDecryptRom += (_, cmdParams) => _api.DecryptRom(cmdParams);
        cli.OnScrambleRom += (_, cmdParams) => _api.ScrambleRom(cmdParams);
        cli.OnUnscrambleRom += (_, cmdParams) => _api.UnscrambleRom(cmdParams);
        cli.OnDumpCheats += (_, cmdParams) => _api.DumpCheats(cmdParams);
        cli.OnCopyCheats += (_, cmdParams) => _api.CopyCheats(cmdParams);
        return await cli.RootCommand.InvokeAsync(_args);
    }

    private void PrintBanner(CmdParams cmdParams)
    {
        _printer = new TerminalPrinter(printFormat: cmdParams.PrintFormatId);
        _printer.PrintBanner(cmdParams);
    }

    private void PrintFileInfo(InfoCmdParams cmdParams)
    {
        if (cmdParams.PrintFormatId is PrintFormatId.Json)
        {
            HammerheadDump dump = _api.GetDump(cmdParams, full: false);
            var formatter = new JsonFormatter(
                JsonFormatter.Settings.Default
                    .WithIndentation()
                    .WithFormatDefaultValues(true)
                    .WithPreserveProtoFieldNames(true)
            );
            _printer.PrintJson(formatter, dump);
        }
        else
        {
            List<ICodec> codecs = _api.ParseFiles(cmdParams);
            foreach (ICodec codec in codecs)
            {
                _printer = new TerminalPrinter(codec, cmdParams.PrintFormatId);
                _printer.PrintFileInfo(codec.Metadata.FilePath, cmdParams);
            }
        }
    }
}
