// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

#include "DecoderContext.h"
#include "MusicProviderContext.h"

Command Context::PopCommand()
{
    std::lock_guard<std::mutex> guard(_commandMutex);
    if (!_commandQueue.empty())
    {
        Command command = _commandQueue.front();
        _commandQueue.erase(_commandQueue.begin());
        return command;
    }
    return Command::NONE;
}

Command Context::PeekCommand() const
{
    std::lock_guard<std::mutex> guard(_commandMutex);
    if (!_commandQueue.empty())
    {
        return _commandQueue.front();
    }
    return Command::NONE;
}

void Context::QueueCommand(Command command)
{
    std::lock_guard<std::mutex> guard(_commandMutex);
    _commandQueue.push_back(command);
}
