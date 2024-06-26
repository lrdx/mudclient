﻿namespace Adan.Client.MessageDeserializers
{
    #region Namespace Imports

    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Common.Conveyor;
    using Common.MessageDeserializers;
    using Common.Messages;
    using Common.Themes;
    using CSLib.Net.Annotations;
    using CSLib.Net.Diagnostics;
    using Messages;

    #endregion

    /// <summary>
    /// Plain text message processor.
    /// </summary>
    public class TextMessageDeserializer : MessageDeserializer
    {
        #region Constants and Fields

        private const int _asciForeGroundCodeBase = 30;
        private const int _asciBackGroundCodeBase = 40;

        private const char _asciCsiCharacter = '[';
        private const char _asciEscapeCharacter = '\x1B';
        private const char _asciEscapeSequenceCompleteCharacter = 'm';
        private const char _asciEscapeSequenceSeparatorCharacter = ';';

        private readonly char[] _charBuffer = new char[32767];

        private readonly Encoding _encoding = Encoding.GetEncoding(1251);
        private readonly IList<TextMessageBlock> _messageBlocks = new List<TextMessageBlock>();
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private AnsiColor _currentBackColor = AnsiColor.None;
        private AnsiColor _currentForeColor = AnsiColor.None;

        private char _firstEscapeParamChar = '0';
        private bool _hasFirstEscapeParamChar;
        private bool _hasSecondEsapeParamChar;
        private bool _isAfterCsi;
        private bool _isAfterEscapeChar;
        private bool _isBright;
        private char _secondEscapeParamChar;

        #endregion


        public TextMessageDeserializer(MessageConveyor conveyor) : base(conveyor)
        {
        }

        #region Properties

        /// <summary>
        /// Gets the type of deserialized message.
        /// </summary>
        /// <value>
        /// The type of deserialized message.
        /// </value>
        public override int DeserializedMessageType
        {
            get
            {
                return BuiltInMessageTypes.TextMessage;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Processes the data from server.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <param name="bytesReceived">The bytes received.</param>
        /// <param name="data">The get data.</param>
        /// <param name="isComplete">Indicates whether message should be completed or wait for next data.</param>
        public override void DeserializeDataFromServer(int offset, int bytesReceived, [NotNull] byte[] data, bool isComplete)
        {
            Assert.ArgumentNotNull(data, "data");

            if (bytesReceived <= 0)
                return;

            int currentDataBufferPosition = 0;

            var incomingCharsCount = _encoding.GetChars(data, offset, bytesReceived, _charBuffer, 0);
            while (currentDataBufferPosition < incomingCharsCount)
            {
                char currentChar = _charBuffer[currentDataBufferPosition];
                if (currentChar == _asciEscapeCharacter)
                {
                    ClearSequence();
                    _isAfterEscapeChar = true;
                }
                else if (_isAfterEscapeChar)
                {
                    if (_isAfterCsi)
                    {
                        if (currentChar == _asciEscapeSequenceCompleteCharacter)
                        {
                            FlushCurrentBlockToBlocksList();
                            ProcessEcapeSequence();
                            ClearSequence();
                        }
                        else if (currentChar == _asciEscapeSequenceSeparatorCharacter)
                        {
                            FlushCurrentBlockToBlocksList();
                            ProcessEcapeSequence();
                            _hasFirstEscapeParamChar = false;
                            _hasSecondEsapeParamChar = false;
                        }
                        else
                        {
                            if (!_hasFirstEscapeParamChar)
                            {
                                _hasFirstEscapeParamChar = true;
                                _firstEscapeParamChar = currentChar;
                            }
                            else
                            {
                                _hasSecondEsapeParamChar = true;
                                _secondEscapeParamChar = currentChar;
                            }
                        }
                    }
                    else if(currentChar == _asciCsiCharacter)
                    {
                        _isAfterCsi = true;
                    }
                    else
                    {
                        // error wrong ascii sequence - flushing content.
                        FlushCurrentBlockToBlocksList();
                        ClearSequence();
                    }
                }
                else
                {
                    if (currentChar == '\n')
                    {
                        FlushCurrentBlockToBlocksList();
                        // end of line achieved - flushing current text.
                        FlushCurrentLineToConveyor();
                    }
                    else if (currentChar != '\r')
                    {
                        _stringBuilder.Append(currentChar);
                    }
                }

                currentDataBufferPosition++;
            }
        }
        
        #endregion

        #region Methods

        private void ClearSequence()
        {
            _isAfterEscapeChar = false;
            _isAfterCsi = false;
            _firstEscapeParamChar = '0';
            _hasSecondEsapeParamChar = false;
            _hasFirstEscapeParamChar = false;
        }

        private void FlushCurrentBlockToBlocksList()
        {
            if (_stringBuilder.Length > 0)
            {
                var textblock = new TextMessageBlock(_stringBuilder.ToString(), ConvertAnsiColorToTextColor(_currentForeColor, _isBright), ConvertAnsiColorToTextColor(_currentBackColor, _isBright));
                _messageBlocks.Add(textblock);
                _stringBuilder.Clear();
            }
        }

        private void FlushCurrentLineToConveyor()
        {
            PushMessageToConveyor(new OutputToMainWindowMessage(new List<TextMessageBlock>(_messageBlocks)));
            _messageBlocks.Clear();
        }

        /// <summary>
        /// Processes the ecape sequence.
        /// </summary>
        /// <exception cref="InvalidDataException"><c>InvalidDataException</c>.</exception>
        private void ProcessEcapeSequence()
        {
            int sequenceValue = 0;
            if (_hasFirstEscapeParamChar)
            {
                sequenceValue += _firstEscapeParamChar - '0';
            }

            if (_hasSecondEsapeParamChar)
            {
                sequenceValue = (sequenceValue * 10) + (_secondEscapeParamChar - '0');
            }

            if (sequenceValue == 0)
            {
                _isBright = false;
                _currentForeColor = AnsiColor.None;
                _currentBackColor = AnsiColor.None;
            }
            else if (sequenceValue == 1)
            {
                _isBright = true;
            }
            else if (sequenceValue >= _asciBackGroundCodeBase
                     && sequenceValue <= _asciBackGroundCodeBase + (int)AnsiColor.White)
            {
                _currentBackColor = (AnsiColor)(sequenceValue - _asciBackGroundCodeBase);
            }
            else if (sequenceValue >= _asciForeGroundCodeBase
                     && sequenceValue <= _asciForeGroundCodeBase + (int)AnsiColor.White)
            {
                _currentForeColor = (AnsiColor)(sequenceValue - _asciForeGroundCodeBase);
            }
            else
            {
                throw new InvalidDataException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "ASCI escape sequence has invalid parameter - '{0}'",
                        sequenceValue));
            }
        }

        private static TextColor ConvertAnsiColorToTextColor(AnsiColor ansiColor, bool isBright)
        {
            if (isBright)
            {
                switch (ansiColor)
                {
                    case AnsiColor.Black:
                        return TextColor.BrightBlack;
                    case AnsiColor.Blue:
                        return TextColor.BrightBlue;
                    case AnsiColor.Cyan:
                        return TextColor.BrightCyan;
                    case AnsiColor.Green:
                        return TextColor.BrightGreen;
                    case AnsiColor.Magenta:
                        return TextColor.BrightMagenta;
                    case AnsiColor.Red:
                        return TextColor.BrightRed;
                    case AnsiColor.White:
                        return TextColor.BrightWhite;
                    case AnsiColor.Yellow:
                        return TextColor.BrightYellow;
                }
            }
            else
            {
                switch (ansiColor)
                {
                    case AnsiColor.Black:
                        return TextColor.Black;
                    case AnsiColor.Blue:
                        return TextColor.Blue;
                    case AnsiColor.Cyan:
                        return TextColor.Cyan;
                    case AnsiColor.Green:
                        return TextColor.Green;
                    case AnsiColor.Magenta:
                        return TextColor.Magenta;
                    case AnsiColor.Red:
                        return TextColor.Red;
                    case AnsiColor.White:
                        return TextColor.White;
                    case AnsiColor.Yellow:
                        return TextColor.Yellow;
                }
            }

            return TextColor.None;
        }

        #endregion
    }
}