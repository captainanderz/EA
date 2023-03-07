export class Patterns {
  static readonly email = '[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,20}$';
  static readonly password =
    '^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*([^\\w\\s]|[_])).{6,}$';
  static readonly phoneNumber =
    '^\\s*(?:\\+?(\\d{1,3}))?[-. (]*(\\d{3})[-. )]*(\\d{3})[-. ]*(\\d{4})(?: *x(\\d+))?\\s*$';
  static readonly mfaCode = '^\\d{3}[ -]?\\d{3}$';
  static readonly notOnlyWhiteSpace = '.*\\S.*';
  static readonly personName = "^[\\p{L},.' -]+$"; // \p{L} is any letter from any language
  static readonly companyName = "^[\\p{L}\\p{N},.' \\/-]+$"; // \p{N} is any numeric character
}
