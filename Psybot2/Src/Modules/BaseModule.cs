using System;
using Discord;
using Discord.WebSocket;

namespace Psybot2.Src.Modules
{
    internal abstract class BaseModule
    {
        /// <summary> Имя мода. </summary>
        public readonly string ModName;

        /// <summary> Комманда, по которой вызывается OnGetMessage. </summary>
        public readonly string CommandName;

        /// <summary> Принимать все сообщения. </summary>
        public bool ReceiveAllMessages;

        /// <summary> Не выводить имя комманды в справке. </summary>
        public bool Hidden;

        /// <summary> Комманду может вызвать только админ бота. </summary>
        public bool AdminOnly;

        /// <summary> Получать события об реакциях в сообщениях. </summary>
        public bool Reaction;

        protected IPsyClient psybot;

        public bool IsEnable { get; private set; }

        /// <summary> Базовый класс для модулей. </summary>
        /// <param name="commandName">Комманда для вызова метода OnGetMessage(true).</param>
        /// <param name="receiveAllMessages">Получать все сообщения(false).</param>
        /// <param name="hidden">Не выводить имя комманды в справке.</param>
        /// <param name="adminOnly">Комманду может вызвать только админ бота.</param>
        protected BaseModule(string modName, string commandName)
        {
            ModName = modName;
            CommandName = commandName;
        }

        protected void Log(string text, Exception exc = null)
        {
            PsyClient.CustomLog(ModName + " -> " + text, CustomLogEnum.Module, exc);
        }

        public void Init(IPsyClient psy)
        {
            if (ModName == null)
                throw new Exception("ModName is null.");

            psybot = psy;
        }

        /// <summary> Вызывается при получении сообщения. </summary>
        /// <param name="triggered">Когда вызвано коммандой.</param>
        /// <param name="args">Аргументы (если имеются)(исключает префик и комманду).</param>
        public abstract void OnGetMessage(bool triggered, SocketMessage mess, string[] args);

        public virtual void OnEnable() => IsEnable = true;

        public virtual void OnDisable() => IsEnable = false;

        public virtual void OnReactionAdded(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        { }

        public virtual void OnReactionRemoved(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        { }

    }
}
