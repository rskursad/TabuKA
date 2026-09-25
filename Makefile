# ============================================================
#  TabuKA Makefile - Linux / macOS / Windows (Git-Bash, WSL)
#  Windows için PowerShell alternatifi: TabuKA.ps1
# ============================================================

PROJECT       := TabuKA.csproj
TEST_PROJ     := TabuKA.Tests/TabuKA.Tests.csproj
ANDROID_PROJ  := TabuKA.Android/TabuKA.Android.csproj
CONFIG        ?= Debug
OUTPUT_DIR    := publish

# ---- Android İmza (Keystore) Yapılandırması ----
# Eğer keystore.properties mevcutsa yükle
-include keystore.properties

# Öncelik sırası: CLI parametresi > keystore.properties > kullanıcı promptu
# Şifreler asla varsayılan/dosyada hazır girilmez (ortak proje): CLI veya
# keystore.properties ile verilmezse imza komutları çalışırken sorulur.
KEYSTORE        ?= $(if $(storeFile),$(storeFile),tabuka.keystore)
KEY_ALIAS       ?= $(if $(keyAlias),$(keyAlias),tabuka)
KEYSTORE_PASS   ?= $(storePassword)
KEY_PASS        ?= $(keyPassword)
ANDROID_FORMAT  ?= apk
KEYSTORE_DNAME  ?= CN=TabuKA, OU=Gaming, O=TabuKA, L=Istanbul, ST=Istanbul, C=TR

# ---- Platform tespiti ----
ifeq ($(OS),Windows_NT)
PLATFORM := windows
DOTNET_FRAMEWORK := net10.0-windows
else
UNAME_S := $(shell uname -s)
ifeq ($(UNAME_S),Darwin)
PLATFORM := macos
DOTNET_FRAMEWORK := net10.0
else
PLATFORM := linux
DOTNET_FRAMEWORK := net10.0
endif
endif

# ---- Renkli çıktı ----
C_RESET  := \033[0m
C_CYAN   := \033[36m
C_GREEN  := \033[32m
C_RED    := \033[31m
C_YELLOW := \033[33m

define banner
	@printf "$(C_CYAN)==========================================\n$(C_RESET)"
	@printf "$(C_YELLOW)  TabuKA - $(1)\n$(C_RESET)"
	@printf "$(C_CYAN)==========================================\n$(C_RESET)"
endef

.PHONY: all help build run test ci publish \
        publish-linux publish-win publish-mac \
        workload-android android-build android-run apk \
        android-keystore keystore android-keystore-custom keystore-custom keystore-interactive \
        android-publish publish-android \
        android-publish-apk android-publish-bundle clean

all: build

help:
	@echo "TabuKA - Make komutları"
	@echo "------------------------"
	@echo "  make build                  Masaüstü derle (varsayılan: Debug)"
	@echo "  make run                    Masaüstü uygulamayı çalıştır"
	@echo "  make test                   Birim testlerini çalıştır"
	@echo "  make ci                     Derle + test et"
	@echo "  make publish                Çalıştığı masaüstü platformu için yayınla"
	@echo "  make publish-linux          Linux paketi (linux-x64, self-contained)"
	@echo "  make publish-win            Windows paketi (win-x64)"
	@echo "  make publish-mac            macOS paketi (osx-x64)"
	@echo ""
	@echo "  Android Komutları:"
	@echo "  make workload-android        Android iş yükünü kur (dotnet workload install android)"
	@echo "  make android-build           Android APK derle (Debug/Release)"
	@echo "  make android-run             Bağlı cihaz veya emülatörde çalıştır"
	@echo "  make android-keystore        Test imza anahtarı oluştur (şifre sorulur)"
	@echo "  make android-keystore-custom Kişisel bilgilerinizle interaktif imza anahtarı oluştur"
	@echo "  make android-publish         İmzalı Android paketi yayınla (şifre sorulur; keystore.properties desteği var)"
	@echo "  make android-publish-apk     İmzalı APK paketi yayınla (şifre sorulur)"
	@echo "  make android-publish-bundle  İmzalı AAB paket (Google Play Bundle) yayınla (şifre sorulur)"
	@echo ""
	@echo "  make clean                   Derleme ve publish klasörlerini temizle"
	@echo ""
	@echo "  Değişkenler (veya keystore.properties dosyasından) — şifreler hazır girilmez:"
	@echo "    CONFIG=Release             Derleme türü (Debug/Release)"
	@echo "    KEYSTORE=tabuka.keystore   Keystore dosya yolu"
	@echo "    KEY_ALIAS=tabuka           Anahtar takma adı"
	@echo "    KEYSTORE_PASS=...          Keystore şifresi (verilmezse sorulur)"
	@echo "    KEY_PASS=...               Anahtar şifresi (verilmezse keystore şifresi kullanılır)"
	@echo "    ANDROID_FORMAT=apk         Paket formatı (apk / aab)"
	@echo "  Aktif platform: $(PLATFORM)"

build:
	$(call banner,Derleniyor [$(CONFIG)] - $(PLATFORM))
	dotnet build $(PROJECT) -c $(CONFIG)

run:
	$(call banner,Oyun Başlatılıyor [$(CONFIG)] - $(PLATFORM))
	dotnet run --project $(PROJECT) -c $(CONFIG) -f $(DOTNET_FRAMEWORK)

test:
	$(call banner,Birim Testleri [$(CONFIG)])
	dotnet test $(TEST_PROJ) -c $(CONFIG)

ci: build test

publish:
	$(call banner,Publish - $(PLATFORM))
	dotnet publish $(PROJECT) -c Release -f net10.0 -o $(OUTPUT_DIR)/$(PLATFORM)
	@echo ""
	@printf "$(C_GREEN)✅ Paket hazır: $(OUTPUT_DIR)/$(PLATFORM)$(C_RESET)\n"
	@printf "$(C_YELLOW)   Çalıştırmak için: ./$(OUTPUT_DIR)/$(PLATFORM)/$(PROJECT:.csproj=)$(C_RESET)\n"

publish-linux:
	$(call banner,Publish - Linux (self-contained))
	dotnet publish $(PROJECT) -c Release -f net10.0 -r linux-x64 --self-contained true -o $(OUTPUT_DIR)/linux

publish-win:
	$(call banner,Publish - Windows (win-x64))
	@if [ "$(PLATFORM)" != "windows" ]; then \
		printf "$(C_RED)❌ Windows paketi yalnızca Windows üzerinde derlenebilir.\n$(C_RESET)"; \
		exit 1; \
	fi
	dotnet publish $(PROJECT) -c Release -f net10.0-windows -r win-x64 --self-contained false -o $(OUTPUT_DIR)/win

publish-mac:
	$(call banner,Publish - macOS (osx-x64))
	@if [ "$(PLATFORM)" != "macos" ]; then \
		printf "$(C_RED)❌ macOS paketi yalnızca macOS üzerinde derlenebilir.\n$(C_RESET)"; \
		exit 1; \
	fi
	dotnet publish $(PROJECT) -c Release -f net10.0 -r osx-x64 --self-contained true -o $(OUTPUT_DIR)/macos

workload-android:
	$(call banner,Android İş Yükü Kurulumu)
	dotnet workload install android

android-build:
	$(call banner,Android APK Derleniyor [$(CONFIG)])
	@if dotnet workload list 2>/dev/null | grep -qi "android"; then \
		dotnet build $(ANDROID_PROJ) -c $(CONFIG); \
	else \
		printf "$(C_RED)❌ 'android' iş yükü kurulu değil!\n$(C_RESET)"; \
		printf "$(C_YELLOW)   Kurulum: make workload-android\n$(C_RESET)"; \
		exit 1; \
	fi

apk: android-build

android-run:
	$(call banner,Android Cihazda Çalıştırılıyor [$(CONFIG)])
	@if command -v adb >/dev/null 2>&1; then \
		adb devices; \
	else \
		printf "$(C_YELLOW)⚠️ adb PATH'te bulunamadı.\n$(C_RESET)"; \
	fi
	dotnet build $(ANDROID_PROJ) -t:Run -c $(CONFIG)

android-keystore keystore:
	$(call banner,Android Keystore Kontrolü / Oluşturma)
	@if [ -f "$(KEYSTORE)" ]; then \
		printf "$(C_YELLOW)ℹ️ Keystore dosyası zaten mevcut: $(KEYSTORE)$(C_RESET)\n"; \
	else \
		if command -v keytool >/dev/null 2>&1; then \
			printf "$(C_CYAN)Yeni test imza anahtarı oluşturuluyor. Keystore ve anahtar şifreniz keytool tarafından sorulacaktır.$(C_RESET)\n\n"; \
			keytool -genkeypair -v -keystore $(KEYSTORE) -alias $(KEY_ALIAS) \
				-keyalg RSA -keysize 2048 -validity 10000 \
				-dname "$(KEYSTORE_DNAME)"; \
			printf "$(C_GREEN)✅ Yeni test imza anahtarı oluşturuldu: $(KEYSTORE)$(C_RESET)\n"; \
			printf "$(C_YELLOW)   Alias: $(KEY_ALIAS)$(C_RESET)\n"; \
		else \
			printf "$(C_RED)❌ 'keytool' komutu bulunamadı! Lütfen JDK veya Android Studio kurun.$(C_RESET)\n"; \
			exit 1; \
		fi \
	fi

android-keystore-custom keystore-custom keystore-interactive:
	$(call banner,Kişisel Android İmza Anahtarı (Keystore) Oluşturma)
	@if command -v keytool >/dev/null 2>&1; then \
		printf "$(C_CYAN)Kendi imzanız için bilgileriniz etkileşimli olarak sorulacaktır.$(C_RESET)\n\n"; \
		read -p "Keystore dosya adı [$(KEYSTORE)]: " ks; \
		KS=$${ks:-$(KEYSTORE)}; \
		read -p "Anahtar takma adı (Alias) [$(KEY_ALIAS)]: " al; \
		AL=$${al:-$(KEY_ALIAS)}; \
		if [ -f "$$KS" ]; then \
			printf "$(C_YELLOW)⚠️ '$$KS' dosyası zaten mevcut! Üzerine yazmak istiyor musunuz? (e/H): $(C_RESET)"; \
			read -r ans; \
			if [ "$$ans" != "e" ] && [ "$$ans" != "E" ] && [ "$$ans" != "y" ] && [ "$$ans" != "Y" ]; then \
				printf "$(C_YELLOW)İşlem iptal edildi.$(C_RESET)\n"; \
				exit 0; \
			fi; \
			rm -f "$$KS"; \
		fi; \
		printf "\n$(C_GREEN)▶️ keytool başlatılıyor (Lütfen şifrenizi ve kimlik bilgilerinizi girin):$(C_RESET)\n\n"; \
		keytool -genkeypair -v -keystore "$$KS" -alias "$$AL" -keyalg RSA -keysize 2048 -validity 10000; \
		if [ -f "$$KS" ]; then \
			printf "\n$(C_GREEN)✅ Kişisel imza anahtarınız başarıyla oluşturuldu: $$KS$(C_RESET)\n"; \
			printf "$(C_YELLOW)   Alias: $$AL$(C_RESET)\n\n"; \
			printf "$(C_CYAN)Bu anahtar dosyasını 'keystore.properties' içine kaydetmek ister misiniz? (E/h): $(C_RESET)"; \
			read -r save_prop; \
			if [ "$$save_prop" != "h" ] && [ "$$save_prop" != "H" ] && [ "$$save_prop" != "n" ] && [ "$$save_prop" != "N" ]; then \
				printf "storeFile=$$KS\nkeyAlias=$$AL\n# storePassword=şifreniz\n# keyPassword=şifreniz\n" > keystore.properties; \
				printf "$(C_GREEN)✅ 'keystore.properties' kaydedildi.$(C_RESET)\n"; \
			fi; \
		fi; \
	else \
		printf "$(C_RED)❌ 'keytool' komutu bulunamadı! Lütfen JDK veya Android Studio kurun.$(C_RESET)\n"; \
		exit 1; \
	fi

android-publish publish-android:
	$(call banner,İmzalı Android Paketi Yayınlanıyor [$(ANDROID_FORMAT)])
	@if ! dotnet workload list 2>/dev/null | grep -qi "android"; then \
		printf "$(C_RED)❌ 'android' iş yükü kurulu değil!\n$(C_RESET)"; \
		printf "$(C_YELLOW)   Kurulum: make workload-android\n$(C_RESET)"; \
		exit 1; \
	fi
	@if [ ! -f "$(KEYSTORE)" ]; then \
		printf "$(C_RED)❌ İmza anahtarı ($(KEYSTORE)) bulunamadı!$(C_RESET)\n"; \
		printf "$(C_CYAN)  • Kendi imzanızı oluşturmak için:      make android-keystore-custom\n"; \
		printf "  • Var olan anahtarınızı bağlamak için: 'keystore.properties' dosyasını düzenleyin\n"; \
		printf "  • Otomatik test anahtarı üretmek için: make android-keystore\n$(C_RESET)"; \
		exit 1; \
	fi
	@if [ -z "$(KEYSTORE_PASS)" ]; then \
		printf "$(C_YELLOW)🔑 Keystore şifresi (gizli): $(C_RESET)"; \
		stty -echo; \
		read -r SP; \
		rc=$$?; \
		stty echo; \
		printf "\n"; \
		if [ $$rc -ne 0 ]; then \
			printf "$(C_RED)❌ Girdi okunamadı!\n$(C_RESET)"; \
			exit $$rc; \
		fi; \
	else \
		SP="$(KEYSTORE_PASS)"; \
	fi; \
	if [ -z "$$SP" ]; then \
		printf "$(C_RED)❌ Keystore şifresi boş olamaz! 'make android-publish KEYSTORE_PASS=...' ile de verebilirsiniz.\n$(C_RESET)"; \
		exit 1; \
	fi; \
	if [ -n "$(KEY_PASS)" ]; then \
		KP="$(KEY_PASS)"; \
	else \
		KP="$$SP"; \
	fi; \
	mkdir -p $(OUTPUT_DIR)/android; \
	dotnet publish $(ANDROID_PROJ) -c Release -f net10.0-android \
		-p:AndroidKeyStore=true \
		-p:AndroidSigningKeyStore="$(abspath $(KEYSTORE))" \
		-p:AndroidSigningStorePass="$$SP" \
		-p:AndroidSigningKeyAlias="$(KEY_ALIAS)" \
		-p:AndroidSigningKeyPass="$$KP" \
		-p:AndroidPackageFormat="$(ANDROID_FORMAT)" \
		-o $(OUTPUT_DIR)/android
	@echo ""
	@printf "$(C_GREEN)✅ İmzalı Android paketi hazır: $(OUTPUT_DIR)/android$(C_RESET)\n"
	@find $(OUTPUT_DIR)/android TabuKA.Android/bin/Release/net10.0-android -name "*Signed*" -o -name "*.apk" -o -name "*.aab" 2>/dev/null | sort -u | while read -r f; do \
		printf "$(C_YELLOW)   📱 $$f ($$(du -h "$$f" 2>/dev/null | cut -f1))$(C_RESET)\n"; \
	done

android-publish-apk:
	$(MAKE) android-publish ANDROID_FORMAT=apk

android-publish-bundle:
	$(MAKE) android-publish ANDROID_FORMAT=aab

clean:
	$(call banner,Temizlik)
	dotnet clean $(PROJECT) -c $(CONFIG) -v q
	rm -rf bin obj
	rm -rf TabuKA.Android/bin TabuKA.Android/obj
	rm -rf TabuKA.Tests/bin TabuKA.Tests/obj
	rm -rf $(OUTPUT_DIR)
	@printf "$(C_GREEN)✅ Temizlendi.\n$(C_RESET)"