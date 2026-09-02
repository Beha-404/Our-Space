namespace OurSpace.API.Common.Localization;

public static class Messages
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> All = new Dictionary<string, IReadOnlyDictionary<string, string>>
    {
        ["Auth.InvalidEmail"] = Lang(
            "Email adresa nije validna.",
            "Please enter a valid email address.",
            "Introduce un correo electrónico válido."),
        ["Auth.WeakPassword"] = Lang(
            "Lozinka mora imati najmanje 9 karaktera, jedno veliko slovo, jedno malo slovo i jedan broj.",
            "Password must be at least 9 characters with one uppercase letter, one lowercase letter and one number.",
            "La contraseña debe tener al menos 9 caracteres, una mayúscula, una minúscula y un número."),
        ["Auth.UsernameTaken"] = Lang(
            "Korisničko ime je već zauzeto.",
            "That username is already taken.",
            "Ese nombre de usuario ya está en uso."),
        ["Auth.EmailTaken"] = Lang(
            "Email je već registrovan.",
            "That email is already registered.",
            "Ese correo ya está registrado."),
        ["Auth.LoginFailed"] = Lang(
            "Pogrešno korisničko ime ili lozinka.",
            "Wrong username or password.",
            "Usuario o contraseña incorrectos."),
        ["Auth.RegistrationClosed"] = Lang(
            "Registracija je zatvorena.",
            "Registration is closed.",
            "El registro está cerrado."),
        ["Auth.InvalidResetCode"] = Lang(
            "Nevažeći ili istekao kod za resetovanje lozinke.",
            "Invalid or expired password reset code.",
            "Código de restablecimiento inválido o expirado."),
        ["Auth.InvalidLoginCode"] = Lang(
            "Nevažeći ili istekao kod za prijavu.",
            "Invalid or expired sign-in code.",
            "Código de acceso inválido o expirado."),
        ["Auth.InvalidRefreshToken"] = Lang(
            "Nevažeći ili istekao refresh token.",
            "Invalid or expired refresh token.",
            "Token de actualización inválido o expirado."),

        ["User.PictureFileEmpty"] = Lang(
            "Fajl je prazan.",
            "The file is empty.",
            "El archivo está vacío."),
        ["User.PictureTooLarge"] = Lang(
            "Slika je prevelika (maksimalno 5MB).",
            "The picture is too large (max 5MB).",
            "La imagen es demasiado grande (máx. 5MB)."),
        ["User.PictureUnsupportedFormat"] = Lang(
            "Nepodržan format slike. Dozvoljeno: JPEG, PNG, WEBP, GIF.",
            "Unsupported picture format. Allowed: JPEG, PNG, WEBP, GIF.",
            "Formato de imagen no compatible. Permitidos: JPEG, PNG, WEBP, GIF."),
        ["User.NotFound"] = Lang(
            "Korisnik nije pronađen.",
            "User not found.",
            "Usuario no encontrado."),
        ["User.AlreadyPaired"] = Lang(
            "Već si uparen/a sa partnerom.",
            "You're already paired with a partner.",
            "Ya estás emparejado/a con una pareja."),
        ["User.InvalidPairingCode"] = Lang(
            "Nevažeći ili istekao kod za uparivanje.",
            "Invalid or expired pairing code.",
            "Código de emparejamiento inválido o expirado."),
        ["User.CannotPairSelf"] = Lang(
            "Ne možeš se upariti sam sa sobom.",
            "You can't pair with yourself.",
            "No puedes emparejarte contigo mismo/a."),
        ["User.NotPaired"] = Lang(
            "Nisi uparen/a sa partnerom.",
            "You're not paired with a partner.",
            "No estás emparejado/a con una pareja."),
        ["User.FileRequired"] = Lang(
            "Fajl je obavezan.",
            "A file is required.",
            "Se requiere un archivo."),
        ["User.UnsupportedLanguage"] = Lang(
            "Nepodržan jezik.",
            "Unsupported language.",
            "Idioma no compatible."),
        ["User.UsernameRequired"] = Lang(
            "Korisničko ime je obavezno.",
            "A username is required.",
            "El nombre de usuario es obligatorio."),
        ["User.SameEmail"] = Lang(
            "To je već tvoja trenutna email adresa.",
            "That's already your current email address.",
            "Ese ya es tu correo electrónico actual."),
        ["User.NoPendingEmailChange"] = Lang(
            "Nema email adrese koja čeka potvrdu.",
            "There's no email change waiting for confirmation.",
            "No hay ningún cambio de correo pendiente de confirmación."),
        ["User.InvalidEmailChangeCode"] = Lang(
            "Nevažeći ili istekao kod za potvrdu.",
            "Invalid or expired confirmation code.",
            "Código de confirmación inválido o expirado."),

        ["Event.TitleRequired"] = Lang(
            "Naziv događaja je obavezan.",
            "An event title is required.",
            "El título del evento es obligatorio."),
        ["Event.NotFound"] = Lang(
            "Događaj nije pronađen.",
            "Event not found.",
            "Evento no encontrado."),
        ["Event.NeedPartner"] = Lang(
            "Moraš biti uparen/a sa partnerom da bi dodao/la događaj.",
            "You need to be paired with a partner to add an event.",
            "Debes estar emparejado/a con una pareja para agregar un evento."),

        ["Photo.FileEmpty"] = Lang(
            "Fajl je prazan.",
            "The file is empty.",
            "El archivo está vacío."),
        ["Storage.QuotaExceeded"] = Lang(
            "Nema više prostora. Ograničenje je {0}MB, a preostalo je {1}MB. Obriši nešto pa pokušaj ponovo.",
            "Out of space. The limit is {0}MB and only {1}MB is left. Delete something and try again.",
            "Sin espacio. El límite es {0}MB y solo quedan {1}MB. Elimina algo e inténtalo de nuevo."),
        ["Photo.TooLarge"] = Lang(
            "Slika je prevelika (maksimalno 10MB).",
            "The picture is too large (max 10MB).",
            "La imagen es demasiado grande (máx. 10MB)."),
        ["Photo.UnsupportedFormat"] = Lang(
            "Nepodržan format slike. Dozvoljeno: JPEG, PNG, WEBP, GIF.",
            "Unsupported picture format. Allowed: JPEG, PNG, WEBP, GIF.",
            "Formato de imagen no compatible. Permitidos: JPEG, PNG, WEBP, GIF."),
        ["Photo.NeedPartner"] = Lang(
            "Moraš biti uparen/a sa partnerom da bi dodavao/la slike.",
            "You need to be paired with a partner to add photos.",
            "Debes estar emparejado/a con una pareja para agregar fotos."),
        ["Photo.NotFound"] = Lang(
            "Slika nije pronađena.",
            "Photo not found.",
            "Foto no encontrada."),
        ["Photo.FileRequired"] = Lang(
            "Fajl je obavezan.",
            "A file is required.",
            "Se requiere un archivo."),
        ["Photo.TitleRequired"] = Lang(
            "Naslov je obavezan.",
            "A title is required.",
            "Se requiere un título."),

        ["Audio.FileEmpty"] = Lang(
            "Fajl je prazan.",
            "The file is empty.",
            "El archivo está vacío."),
        ["Audio.TooLarge"] = Lang(
            "Audio fajl je prevelik (maksimalno 20MB).",
            "The audio file is too large (max 20MB).",
            "El archivo de audio es demasiado grande (máx. 20MB)."),
        ["Audio.UnsupportedFormat"] = Lang(
            "Nepodržan format audio fajla.",
            "Unsupported audio format.",
            "Formato de audio no compatible."),
        ["Audio.ConversionFailed"] = Lang(
            "Konverzija video fajla u audio nije uspjela.",
            "Converting the video file to audio failed.",
            "No se pudo convertir el archivo de video a audio."),
        ["Audio.ConverterNotReady"] = Lang(
            "Video konverzija još nije spremna. Pokušaj ponovo za koji minut.",
            "Video conversion is not ready yet. Try again in a minute.",
            "La conversión de video aún no está lista. Inténtalo de nuevo en un minuto."),
        ["Audio.TitleRequired"] = Lang(
            "Naslov je obavezan.",
            "A title is required.",
            "Se requiere un título."),
        ["Audio.NeedPartner"] = Lang(
            "Moraš biti uparen/a sa partnerom da bi dodavao/la audio poruke.",
            "You need to be paired with a partner to add audio messages.",
            "Debes estar emparejado/a con una pareja para agregar mensajes de audio."),
        ["Audio.NotFound"] = Lang(
            "Audio poruka nije pronađena.",
            "Audio message not found.",
            "Mensaje de audio no encontrado."),
        ["Audio.FileRequired"] = Lang(
            "Fajl je obavezan.",
            "A file is required.",
            "Se requiere un archivo."),

        ["Wish.TitleRequired"] = Lang(
            "Naziv želje je obavezan.",
            "A wish needs a title.",
            "El deseo necesita un título."),
        ["Wish.NotFound"] = Lang(
            "Želja nije pronađena.",
            "Wish not found.",
            "Deseo no encontrado."),
        ["Wish.NeedPartner"] = Lang(
            "Moraš biti uparen/a sa partnerom da bi dodao/la želju.",
            "You need to be paired with a partner to add a wish.",
            "Debes estar emparejado/a con una pareja para agregar un deseo."),

        ["Generic.UnexpectedError"] = Lang(
            "Došlo je do neočekivane greške.",
            "An unexpected error occurred.",
            "Ocurrió un error inesperado."),
        ["Generic.TooManyRequests"] = Lang(
            "Previše pokušaja. Pokušaj ponovo kasnije.",
            "Too many attempts. Please try again later.",
            "Demasiados intentos. Inténtalo de nuevo más tarde."),

        ["Email.LoginCode.Subject"] = Lang(
            "Kod za prijavu",
            "Your sign-in code",
            "Tu código de acceso"),
        ["Email.LoginCode.Body"] = Lang(
            "Neko se pokušava prijaviti na tvoj nalog.\n\nKod za prijavu: {0}\n\nKod ističe za {1} minuta. Ako ovo nisi bio/la ti, neko zna tvoju lozinku — promijeni je odmah.",
            "Someone is signing in to your account.\n\nSign-in code: {0}\n\nThe code expires in {1} minutes. If this wasn't you, someone knows your password — change it right away.",
            "Alguien está iniciando sesión en tu cuenta.\n\nCódigo de acceso: {0}\n\nEl código expira en {1} minutos. Si no fuiste tú, alguien conoce tu contraseña — cámbiala de inmediato."),

        ["Email.PasswordReset.Subject"] = Lang(
            "Resetovanje lozinke",
            "Password reset",
            "Restablecer contraseña"),
        ["Email.PasswordReset.Body"] = Lang(
            "Zatraženo je resetovanje tvoje lozinke.\n\nKod za potvrdu: {0}\n\nKod ističe za {1} minuta. Ako ovo nisi bio/la ti, ignoriši ovu poruku — lozinka ostaje nepromijenjena.",
            "A password reset was requested for your account.\n\nConfirmation code: {0}\n\nThe code expires in {1} minutes. If this wasn't you, just ignore this message — your password stays unchanged.",
            "Se solicitó restablecer la contraseña de tu cuenta.\n\nCódigo de confirmación: {0}\n\nEl código expira en {1} minutos. Si no fuiste tú, ignora este mensaje — tu contraseña no cambiará."),

        ["Email.EmailChange.Subject"] = Lang(
            "Potvrdi promjenu email adrese",
            "Confirm your email change",
            "Confirma el cambio de tu correo"),
        ["Email.EmailChange.Body"] = Lang(
            "Zatražena je promjena email adrese na \"{0}\".\n\nKod za potvrdu: {1}\n\nKod ističe za 24 sata. Ako ovo nisi bio/la ti, ignoriši ovu poruku — adresa se neće promijeniti.",
            "A change of your email address to \"{0}\" was requested.\n\nConfirmation code: {1}\n\nThe code expires in 24 hours. If this wasn't you, just ignore this message — your address won't be changed.",
            "Se solicitó cambiar tu correo electrónico a \"{0}\".\n\nCódigo de confirmación: {1}\n\nEl código expira en 24 horas. Si no fuiste tú, ignora este mensaje — tu dirección no cambiará."),

        ["Email.EventCreated.Subject"] = Lang("Novi događaj: {0}", "New event: {0}", "Nuevo evento: {0}"),
        ["Email.EventCreated.Body"] = Lang(
            "{0} je dodao/la novi događaj \"{1}\".",
            "{0} added a new event \"{1}\".",
            "{0} agregó un nuevo evento \"{1}\"."),
        ["Email.EventUpdated.Subject"] = Lang("Događaj izmijenjen: {0}", "Event updated: {0}", "Evento actualizado: {0}"),
        ["Email.EventUpdated.Body"] = Lang(
            "{0} je izmijenio/la događaj \"{1}\".",
            "{0} updated the event \"{1}\".",
            "{0} actualizó el evento \"{1}\"."),
        ["Email.EventDeleted.Subject"] = Lang("Događaj obrisan: {0}", "Event deleted: {0}", "Evento eliminado: {0}"),
        ["Email.EventDeleted.Body"] = Lang(
            "{0} je obrisao/la događaj \"{1}\".",
            "{0} deleted the event \"{1}\".",
            "{0} eliminó el evento \"{1}\"."),
        ["Email.Reminder.Subject"] = Lang("Podsjetnik: {0}", "Reminder: {0}", "Recordatorio: {0}"),
        ["Email.Reminder.Body"] = Lang(
            "Događaj \"{0}\" je zakazan za {1}.",
            "The event \"{0}\" is scheduled for {1}.",
            "El evento \"{0}\" está programado para {1}."),
    };

    private static IReadOnlyDictionary<string, string> Lang(string bs, string en, string es) =>
        new Dictionary<string, string> { ["bs"] = bs, ["en"] = en, ["es"] = es };
}
