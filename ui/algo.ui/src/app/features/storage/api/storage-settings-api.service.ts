import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiService } from '../../../core/api/api.service';
import {
  FileScanResult,
  StorageSettings,
  UpdateStorageSettingsCommand,
  UploadImageResult
} from '../models/storage.models';

@Injectable({ providedIn: 'root' })
export class StorageSettingsApiService {
  private readonly api = inject(ApiService);

  getSettings(): Observable<StorageSettings> {
    return this.api.get<StorageSettings>('/storage');
  }

  updateSettings(command: UpdateStorageSettingsCommand): Observable<StorageSettings> {
    return this.api.put<StorageSettings, UpdateStorageSettingsCommand>('/storage', command);
  }

  scanFile(file: File): Observable<FileScanResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.api.post<FileScanResult, FormData>('/storage/scanner/scan', formData);
  }

  uploadProductImage(file: File): Observable<UploadImageResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.api.post<UploadImageResult, FormData>('/storage/upload/product-image', formData);
  }

  uploadCategoryImage(file: File): Observable<UploadImageResult> {
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.api.post<UploadImageResult, FormData>('/storage/upload/category-image', formData);
  }
}
