plugins {
    id("com.android.application")
    // The Flutter Gradle Plugin must be applied after the Android and Kotlin Gradle plugins.
    id("dev.flutter.flutter-gradle-plugin")
}

android {
    namespace = "com.autopartshop.autopartshop_mobile"
    compileSdk = flutter.compileSdkVersion
    ndkVersion = flutter.ndkVersion

    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_17
        targetCompatibility = JavaVersion.VERSION_17
        // Required by flutter_local_notifications (uses java.time APIs).
        isCoreLibraryDesugaringEnabled = true
    }

    // Release signing is driven by env vars written by the CI workflow.
    // If the keystore file or any variable is absent the build falls back to
    // debug signing so local `flutter run --release` still works.
    signingConfigs {
        val keystoreFile = file("upload-keystore.jks")
        val storePass = System.getenv("STORE_PASSWORD")
        val keyAlias  = System.getenv("KEY_ALIAS")
        val keyPass   = System.getenv("KEY_PASSWORD")
        if (keystoreFile.exists() && storePass != null && keyAlias != null && keyPass != null) {
            create("release") {
                storeFile     = keystoreFile
                storePassword = storePass
                this.keyAlias = keyAlias
                keyPassword   = keyPass
            }
        }
    }

    defaultConfig {
        applicationId = "com.autopartshop.autopartshop_mobile"
        minSdk = flutter.minSdkVersion
        targetSdk = flutter.targetSdkVersion
        versionCode = flutter.versionCode
        versionName = flutter.versionName
    }

    buildTypes {
        release {
            signingConfig = signingConfigs.findByName("release")
                ?: signingConfigs.getByName("debug")
        }
    }
}

kotlin {
    compilerOptions {
        jvmTarget = org.jetbrains.kotlin.gradle.dsl.JvmTarget.JVM_17
    }
}

flutter {
    source = "../.."
}

dependencies {
    coreLibraryDesugaring("com.android.tools:desugar_jdk_libs:2.1.4")
}
