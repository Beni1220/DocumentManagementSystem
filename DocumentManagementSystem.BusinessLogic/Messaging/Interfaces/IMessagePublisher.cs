using System;
using System.Collections.Generic;
using System.Text;

namespace DocumentManagementSystem.BusinessLogic.Messaging.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync(string queue, object message);
    }
}
