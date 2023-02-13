using System.Collections.Generic;

namespace ShaderMetadataGenerator
{
	/// <summary>
	/// Character stream for parser
	/// </summary>
	class CharacterStream
	{
		public const string NewLine = "\n";


		IEnumerator<string> fetchNext;
		string currentToProcess = string.Empty;
		public bool IsEnd { get; private set; }

		public CharacterStream(IEnumerator<string> fetchNext)
		{
			this.fetchNext = fetchNext;
		}
		public CharacterStream(IEnumerable<string> fetchNext)
		{
			this.fetchNext = fetchNext.GetEnumerator();
		}
		public CharacterStream(string contents)
		{
			var a = new List<string>();
			a.Add(contents);
			this.fetchNext = a.GetEnumerator();
		}

		bool TryFetchNext()
		{
			if (IsEnd) return false;
			IsEnd = !fetchNext.MoveNext();
			if (IsEnd) return false;
			currentToProcess = currentToProcess + fetchNext.Current;
			return true;
		}

		/// <summary>
		/// Look ahead
		/// Returns num characters from front
		/// </summary>
		/// <param name="charsNum"></param>
		/// <returns></returns>
		public string PeekNumChars(int charsNum)
		{
			while (!IsEnd && currentToProcess.Length < charsNum)
				if (!TryFetchNext()) return currentToProcess;

			return currentToProcess.Substring(0, charsNum);
		}

		/// <summary>
		/// Returns index when Token is found, -1 otherwise
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		public int? PeekTryFind(string token)
		{
			var offset = -1;
			do
			{
				++offset;
				while (currentToProcess.Length < offset + token.Length)
					if (!TryFetchNext()) return null;
			}
			while (currentToProcess.Substring(offset, token.Length) != token);

			return offset + token.Length;
		}

		public string PeekUntilNewLine()
		{
			var len = PeekTryFind(NewLine);
			if (len.HasValue)
				return PeekNumChars(len.Value).Trim();
			return null;
		}

		/// <summary>
		/// Consume
		/// Advances char stream num chars
		/// </summary>
		/// <param name="charsNum"></param>
		/// <returns></returns>
		public string EatNumChars(int charsNum)
		{
			while (!IsEnd && currentToProcess.Length < charsNum)
				if (!TryFetchNext()) return currentToProcess;

			var eaten = currentToProcess.Substring(0, charsNum);

			if (currentToProcess.Length == charsNum)
				currentToProcess = string.Empty;
			else
				currentToProcess = currentToProcess.Substring(charsNum);

			return eaten;
		}

		public bool TryEatAllWhitespaces()
		{
			bool atLeastOne = false;
			do
			{
				var p = PeekNumChars(1);
				if (p == null || p.Length == 0) break;
				if (!string.IsNullOrWhiteSpace(p)) break;
				EatNumChars(1);
				atLeastOne = true;
			} while (true);

			return atLeastOne;
		}

		public bool TryEat(string token)
		{
			if (PeekNumChars(token.Length) == token)
			{
				EatNumChars(token.Length);
				return true;
			}

			return false;
		}

		public string EatAllUntilAndExclude(string endToken)
		{
			string eaten = string.Empty;
			while (!IsEnd && PeekNumChars(endToken.Length) != endToken)
				eaten += EatNumChars(1);
			EatNumChars(endToken.Length);
			return eaten;
		}
		public string EatAllUntilAndInclude(string endToken)
		{
			string eaten = string.Empty;
			while (!IsEnd && PeekNumChars(endToken.Length) != endToken)
				eaten += EatNumChars(1);
			eaten += EatNumChars(endToken.Length);
			return eaten;
		}

		public string EatAllUntilNewLine()
		{
			return EatAllUntilAndExclude(NewLine);
		}

		public string EatAllUntiWhiteSpace()
		{
			string eaten = string.Empty;
			while (!string.IsNullOrWhiteSpace(PeekNumChars(1)))
				eaten += EatNumChars(1);

			TryEatAllWhitespaces();

			return eaten;
		}
	}
}