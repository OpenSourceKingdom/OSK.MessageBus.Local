﻿using OSK.Transmissions.Messages.Abstractions;

namespace OSK.MessageBus.Local.UnitTests.Helpers
{
    public class TestMessage : IMessage
    {
        public string TopicId { get; set; }
    }
}
