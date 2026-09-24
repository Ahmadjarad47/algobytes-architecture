export interface S3StorageSettings {
  readonly provider: 's3';
  readonly endpointUrl: string;
  readonly accessKey: string;
  readonly secretKeyMasked: string | null;
  readonly bucketName: string;
  readonly region: string;
  readonly folder: string;
  readonly usePathStyle: boolean;
}

export interface FileScannerSettings {
  readonly enabled: boolean;
  readonly provider: string;
  readonly endpointUrl: string | null;
  readonly apiKey: string | null;
  readonly quarantineFolder: string | null;
}

export interface UploadImageResult {
  readonly url: string;
}

export interface StorageSettings {
  readonly source: 'database';
  readonly updatedAt: string | null;
  readonly storage: S3StorageSettings;
  readonly scanner: FileScannerSettings;
}

export interface UpdateStorageSettingsCommand {
  readonly storage: {
    readonly endpointUrl: string;
    readonly accessKey: string;
    readonly secretKey: string;
    readonly bucketName: string;
    readonly region: string;
    readonly folder: string;
    readonly usePathStyle: boolean;
  };
  readonly scanner: {
    readonly enabled: boolean;
    readonly provider: string;
    readonly endpointUrl: string | null;
    readonly apiKey: string | null;
    readonly quarantineFolder: string | null;
  };
}

export interface FileScanResult {
  readonly fileName: string;
  readonly size: number;
  readonly status: 'clean' | 'infected' | 'failed' | 'pending';
  readonly engine: string;
  readonly message: string;
  readonly scannedAt: string;
}
